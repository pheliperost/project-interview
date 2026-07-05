using System.Net;
using System.Net.Http.Json;
using BlaInterview.Application.DTOs;
using BlaInterview.Domain.Enums;
using BlaInterview.Integration.Tests.Config;

namespace BlaInterview.Integration.Tests.Api;

[Collection(nameof(IntegrationWebTestsFixtureCollection))]
public class TaskOwnershipTests
{
    private readonly IntegrationTestsFixture _fixture;

    public TaskOwnershipTests(IntegrationTestsFixture fixture) => _fixture = fixture;

    [Fact(DisplayName = "Getting own task should return 200.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Get_OwnTask_ShouldReturnOk()
    {
        // Arrange
        var demoClient = await _fixture.CreateAuthenticatedTasksClientAsync();
        var ownTask = await _fixture.GetDemoTaskAsync();

        // Act
        var response = await demoClient.GetAsync($"/api/tasks/{ownTask.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(DisplayName = "Getting another user's task should return 403.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Get_OtherUsersTask_ShouldReturn403()
    {
        // Arrange
        var otherTaskId = await _fixture.GetSeededOtherUserTaskIdAsync();
        var demoClient = await _fixture.CreateAuthenticatedTasksClientAsync();

        // Act
        var response = await demoClient.GetAsync($"/api/tasks/{otherTaskId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertForbiddenErrorAsync(response);
    }

    [Fact(DisplayName = "Updating another user's task should return 403.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Update_OtherUsersTask_ShouldReturn403()
    {
        // Arrange
        var otherTaskId = await _fixture.GetSeededOtherUserTaskIdAsync();
        var demoClient = await _fixture.CreateAuthenticatedTasksClientAsync();
        var body = new UpdateTaskBody(
            "Hijacked title",
            "Should not apply",
            KanbanStatus.Todo,
            TaskPriority.High,
            null);

        // Act
        var response = await demoClient.PutAsJsonAsync($"/api/tasks/{otherTaskId}", body);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertForbiddenErrorAsync(response);
    }

    [Fact(DisplayName = "Deleting another user's task should return 403.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Delete_OtherUsersTask_ShouldReturn403()
    {
        // Arrange
        var otherTaskId = await _fixture.GetSeededOtherUserTaskIdAsync();
        var demoClient = await _fixture.CreateAuthenticatedTasksClientAsync();

        // Act
        var response = await demoClient.DeleteAsync($"/api/tasks/{otherTaskId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertForbiddenErrorAsync(response);
    }

    [Fact(DisplayName = "Reactivating another user's task should return 403.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Reactivate_OtherUsersTask_ShouldReturn403()
    {
        // Arrange
        var otherClient = await _fixture.CreateAuthenticatedTasksClientAsync(
            IntegrationTestsFixture.OtherEmail,
            IntegrationTestsFixture.OtherPassword);
        var listResponse = await otherClient.GetAsync("/api/tasks?status=Completed");
        listResponse.EnsureSuccessStatusCode();
        var list = await listResponse.Content.ReadFromJsonAsync<TaskListResponse>(IntegrationTestsFixture.JsonOptions);
        var completedTask = list?.Items.FirstOrDefault(t => t.Title.StartsWith("[Other]", StringComparison.Ordinal));
        if (completedTask is null)
        {
            throw new InvalidOperationException("Seeded other-user completed task not found.");
        }

        var demoClient = await _fixture.CreateAuthenticatedTasksClientAsync();

        // Act
        var response = await demoClient.PostAsync($"/api/tasks/{completedTask.Id}/reactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertForbiddenErrorAsync(response);
    }

    private static async Task AssertForbiddenErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(IntegrationTestsFixture.JsonOptions);
        Assert.NotNull(body);
        Assert.True(body!.TryGetValue("error", out var error));
        Assert.Contains("do not have access", error, StringComparison.OrdinalIgnoreCase);
    }
}
