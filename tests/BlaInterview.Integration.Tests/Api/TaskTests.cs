using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlaInterview.Application.DTOs;
using BlaInterview.Domain.Enums;
using BlaInterview.Integration.Tests.Config;

namespace BlaInterview.Integration.Tests.Api;

[Collection(nameof(IntegrationWebTestsFixtureCollection))]
public class TaskTests
{
    private readonly IntegrationTestsFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public TaskTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "Creating a task with a valid token should return 201.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_Create_WithValidToken_ShouldReturnCreated()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedClientAsync();
        var request = _fixture.GenerateValidCreateRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact(DisplayName = "Listing tasks should return a paginated response.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_GetList_ShouldReturnPaginatedResponse()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/tasks");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<TaskListResponse>(JsonOptions);
        Assert.NotNull(list);
        Assert.NotEmpty(list!.Items);
        Assert.True(list.TotalCount >= list.Items.Count);
        Assert.Equal(1, list.Page);
        Assert.Equal(100, list.PageSize);
    }

    [Fact(DisplayName = "Searching tasks by title should return only matching items.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_GetList_SearchByTitle_ShouldReturnMatchingOnly()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/tasks?search=API");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<TaskListResponse>(JsonOptions);
        Assert.NotNull(list);
        Assert.NotEmpty(list!.Items);
        Assert.All(list.Items, t => Assert.Contains("API", t.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Filtering tasks by status should return only matching items.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_GetList_FilterByStatus_ShouldReturnMatchingOnly()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/tasks?status=Completed");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<TaskListResponse>(JsonOptions);
        Assert.NotNull(list);
        Assert.NotEmpty(list!.Items);
        Assert.All(list.Items, t => Assert.Equal(KanbanStatus.Completed, t.Status));
    }
}
