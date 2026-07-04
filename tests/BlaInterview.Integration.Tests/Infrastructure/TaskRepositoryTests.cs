using BlaInterview.Application.Queries;
using BlaInterview.Domain.Enums;
using BlaInterview.Integration.Tests.Fixtures;

namespace BlaInterview.Integration.Tests.Infrastructure;

[Collection(nameof(TaskRepositoryCollection))]
public class TaskRepositoryTests
{
    private readonly TaskRepositoryFixtures _fixtures;

    public TaskRepositoryTests(TaskRepositoryFixtures fixtures)
    {
        _fixtures = fixtures;
    }

    [Fact(DisplayName = "Searching tasks by title should return only matching items.")]
    [Trait("Category", "Task Repository")]
    public async Task TaskRepository_GetByUserAsync_SearchByTitle_ShouldReturnMatchingOnly()
    {
        // Arrange
        await _fixtures.SeedDefaultTasksAsync();
        var query = new TaskQuery("API", null, null, null, null, null, 1, 100);

        // Act
        var page = await _fixtures.Repository.GetByUserAsync("user1", query);

        // Assert
        Assert.Single(page.Items);
        Assert.Contains("API", page.Items[0].Title);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact(DisplayName = "Filtering tasks by status should return only matching items.")]
    [Trait("Category", "Task Repository")]
    public async Task TaskRepository_GetByUserAsync_FilterByStatus_ShouldReturnMatchingOnly()
    {
        // Arrange
        await _fixtures.SeedDefaultTasksAsync();
        var query = new TaskQuery(null, new[] { KanbanStatus.Completed }, null, null, null, null, 1, 100);

        // Act
        var page = await _fixtures.Repository.GetByUserAsync("user1", query);

        // Assert
        Assert.All(page.Items, t => Assert.Equal(KanbanStatus.Completed, t.Status));
        Assert.Equal(page.Items.Count, page.TotalCount);
    }

    [Fact(DisplayName = "Pagination should return a single page of results.")]
    [Trait("Category", "Task Repository")]
    public async Task TaskRepository_GetByUserAsync_Pagination_ShouldReturnPage()
    {
        // Arrange
        await _fixtures.SeedDefaultTasksAsync();
        var query = new TaskQuery(null, null, null, null, null, null, 1, 1);

        // Act
        var page = await _fixtures.Repository.GetByUserAsync("user1", query);

        // Assert
        Assert.Single(page.Items);
        Assert.Equal(2, page.TotalCount);
    }
}
