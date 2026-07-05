using System.Net;
using System.Net.Http.Json;
using BlaInterview.Application.DTOs;
using BlaInterview.Domain.Enums;
using BlaInterview.Integration.Tests.Config;

namespace BlaInterview.Integration.Tests.Api;

[Collection(nameof(IntegrationWebTestsFixtureCollection))]
public class TaskCrudTests
{
    private readonly IntegrationTestsFixture _fixture;

    public TaskCrudTests(IntegrationTestsFixture fixture) => _fixture = fixture;

    [Fact(DisplayName = "Getting an owned task by id should return 200.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_GetById_OwnTask_ShouldReturnOk()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedTasksClientAsync();
        var seeded = await _fixture.GetDemoTaskAsync();

        // Act
        var response = await client.GetAsync($"/api/tasks/{seeded.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(IntegrationTestsFixture.JsonOptions);
        Assert.Equal(seeded.Id, task?.Id);
    }

    [Fact(DisplayName = "Getting a missing task should return 404.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_GetById_Missing_ShouldReturn404()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedTasksClientAsync();

        // Act
        var response = await client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertErrorContainsAsync(response, "not found");
    }

    [Fact(DisplayName = "Updating an owned task should return 200.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Update_OwnTask_ShouldReturnOk()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedTasksClientAsync();
        var created = await _fixture.CreateDemoTaskAsync();
        var body = new UpdateTaskBody(
            "Updated title",
            "Updated description",
            KanbanStatus.InProgress,
            TaskPriority.High,
            DateTimeOffset.UtcNow.AddDays(3));

        // Act
        var response = await client.PutAsJsonAsync($"/api/tasks/{created.Id}", body);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(IntegrationTestsFixture.JsonOptions);
        Assert.Equal("Updated title", task?.Title);
        Assert.Equal(KanbanStatus.InProgress, task?.Status);
    }

    [Fact(DisplayName = "Updating a task with past due date should return 400.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Update_PastDueDate_ShouldReturn400()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedTasksClientAsync();
        var created = await _fixture.CreateDemoTaskAsync();
        var body = new UpdateTaskBody(
            created.Title,
            created.Description,
            created.Status,
            created.Priority,
            DateTimeOffset.UtcNow.AddDays(-1));

        // Act
        var response = await client.PutAsJsonAsync($"/api/tasks/{created.Id}", body);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "Updating terminal task status should return 400.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Update_TerminalStatusChange_ShouldReturn400()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedTasksClientAsync();
        var created = await _fixture.CreateDemoTaskAsync();
        var completeBody = new UpdateTaskBody(
            created.Title,
            created.Description,
            KanbanStatus.Completed,
            created.Priority,
            created.DueDate);
        await client.PutAsJsonAsync($"/api/tasks/{created.Id}", completeBody);

        var body = new UpdateTaskBody(
            created.Title,
            created.Description,
            KanbanStatus.Todo,
            created.Priority,
            created.DueDate);

        // Act
        var response = await client.PutAsJsonAsync($"/api/tasks/{created.Id}", body);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorContainsAsync(response, "reactivate");
    }

    [Fact(DisplayName = "Deleting an owned task should return 204.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Delete_OwnTask_ShouldReturnNoContent()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedTasksClientAsync();
        var created = await _fixture.CreateDemoTaskAsync();

        // Act
        var response = await client.DeleteAsync($"/api/tasks/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await client.GetAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact(DisplayName = "Reactivating a completed task should return To Do.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Reactivate_CompletedTask_ShouldReturnTodo()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedTasksClientAsync();
        var created = await _fixture.CreateDemoTaskAsync();
        var completeBody = new UpdateTaskBody(
            created.Title,
            created.Description,
            KanbanStatus.Completed,
            created.Priority,
            created.DueDate);
        await client.PutAsJsonAsync($"/api/tasks/{created.Id}", completeBody);

        // Act
        var response = await client.PostAsync($"/api/tasks/{created.Id}/reactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(IntegrationTestsFixture.JsonOptions);
        Assert.Equal(KanbanStatus.Todo, task?.Status);
    }

    [Fact(DisplayName = "Reactivating an active task should return 400.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Reactivate_ActiveTask_ShouldReturn400()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedTasksClientAsync();
        var active = await _fixture.CreateDemoTaskAsync();

        // Act
        var response = await client.PostAsync($"/api/tasks/{active.Id}/reactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorContainsAsync(response, "reactivated");
    }

    [Fact(DisplayName = "Creating a task with empty title should return 400.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Create_EmptyTitle_ShouldReturn400()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedTasksClientAsync();
        var body = new CreateTaskBody("", null, TaskPriority.Medium, null);

        // Act
        var response = await client.PostAsJsonAsync("/api/tasks", body);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "Creating a task with past due date should return 400.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Create_PastDueDate_ShouldReturn400()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedTasksClientAsync();
        var body = new CreateTaskBody(
            "Past due task",
            "A description",
            TaskPriority.Medium,
            DateTimeOffset.UtcNow.AddDays(-1));

        // Act
        var response = await client.PostAsJsonAsync("/api/tasks", body);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "Reactivating a cancelled task should return To Do.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Reactivate_CancelledTask_ShouldReturnTodo()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedTasksClientAsync();
        var created = await _fixture.CreateDemoTaskAsync();
        var cancelBody = new UpdateTaskBody(
            created.Title,
            created.Description,
            KanbanStatus.Cancelled,
            created.Priority,
            created.DueDate);
        await client.PutAsJsonAsync($"/api/tasks/{created.Id}", cancelBody);

        // Act
        var response = await client.PostAsync($"/api/tasks/{created.Id}/reactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(IntegrationTestsFixture.JsonOptions);
        Assert.Equal(KanbanStatus.Todo, task?.Status);
    }

    private static async Task AssertErrorContainsAsync(HttpResponseMessage response, string fragment)
    {
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(IntegrationTestsFixture.JsonOptions);
        Assert.NotNull(body);
        Assert.True(body!.TryGetValue("error", out var error));
        Assert.Contains(fragment, error, StringComparison.OrdinalIgnoreCase);
    }
}
