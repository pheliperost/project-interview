using System.Net;
using System.Net.Http.Json;
using BlaInterview.Application.DTOs;
using BlaInterview.Integration.Tests.Config;

namespace BlaInterview.Integration.Tests.Api;

[Collection(nameof(IntegrationWebTestsFixtureCollection))]
public class TaskQueryTests
{
    private readonly IntegrationTestsFixture _fixture;

    public TaskQueryTests(IntegrationTestsFixture fixture) => _fixture = fixture;

    [Fact(DisplayName = "Listing tasks with page and page size should return correct metadata.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_GetList_Pagination_ShouldReturnPageMetadata()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedTasksClientAsync();

        // Act
        var response = await client.GetAsync("/api/tasks?page=1&pageSize=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<TaskListResponse>(IntegrationTestsFixture.JsonOptions);
        Assert.NotNull(list);
        Assert.Equal(1, list!.Page);
        Assert.Equal(2, list.PageSize);
        Assert.True(list.Items.Count <= 2);
        Assert.True(list.TotalCount >= list.Items.Count);
    }

    [Fact(DisplayName = "Listing tasks with invalid date range should return 400.")]
    [Trait("Category", "Integration Web - Tasks")]
    public async Task Task_GetList_InvalidDateRange_ShouldReturn400()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedTasksClientAsync();
        var from = DateTimeOffset.UtcNow.ToString("O");
        var to = DateTimeOffset.UtcNow.AddDays(-1).ToString("O");

        // Act
        var response = await client.GetAsync($"/api/tasks?createdFrom={Uri.EscapeDataString(from)}&createdTo={Uri.EscapeDataString(to)}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
