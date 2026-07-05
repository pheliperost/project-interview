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

    [Fact(DisplayName = "UpdateAsync should persist title changes.")]
    [Trait("Category", "Task Repository")]
    public async Task TaskRepository_UpdateAsync_ShouldPersistChanges()
    {
        // Arrange
        await _fixtures.SeedDefaultTasksAsync();
        var existing = _fixtures.Context.Tasks.First(t => t.UserId == "user1");
        existing.Title = "Renamed task";

        // Act
        await _fixtures.Repository.UpdateAsync(existing);
        await _fixtures.Repository.SaveChangesAsync();
        var loaded = await _fixtures.Repository.GetByIdAsync(existing.Id);

        // Assert
        Assert.Equal("Renamed task", loaded?.Title);
    }

    [Fact(DisplayName = "GetByUserAsync should not return another user's tasks.")]
    [Trait("Category", "Task Repository")]
    public async Task TaskRepository_GetByUserAsync_ShouldScopeByUser()
    {
        // Arrange
        await _fixtures.SeedDefaultTasksAsync();
        var query = new TaskQuery(null, null, null, null, null, null, 1, 100);

        // Act
        var page = await _fixtures.Repository.GetByUserAsync("user1", query);

        // Assert
        Assert.Equal(2, page.TotalCount);
        Assert.All(page.Items, t => Assert.Equal("user1", t.UserId));
    }

    [Fact(DisplayName = "GetByIdAsync should return persisted task.")]
    [Trait("Category", "Task Repository")]
    public async Task TaskRepository_GetByIdAsync_ShouldReturnTask()
    {
        // Arrange
        await _fixtures.SeedDefaultTasksAsync();
        var existing = _fixtures.Context.Tasks.First(t => t.UserId == "user1");

        // Act
        var task = await _fixtures.Repository.GetByIdAsync(existing.Id);

        // Assert
        Assert.NotNull(task);
        Assert.Equal(existing.Id, task!.Id);
    }

    [Fact(DisplayName = "Add and delete should round-trip through the database.")]
    [Trait("Category", "Task Repository")]
    public async Task TaskRepository_AddDelete_ShouldRoundTrip()
    {
        // Arrange
        var task = _fixtures.GenerateTask("user1", "Disposable", KanbanStatus.Todo, DateTimeOffset.UtcNow);

        // Act
        await _fixtures.Repository.AddAsync(task);
        await _fixtures.Repository.SaveChangesAsync();
        var loaded = await _fixtures.Repository.GetByIdAsync(task.Id);
        await _fixtures.Repository.DeleteAsync(loaded!);
        await _fixtures.Repository.SaveChangesAsync();

        // Assert
        Assert.NotNull(loaded);
        Assert.Null(await _fixtures.Repository.GetByIdAsync(task.Id));
    }

    [Fact(DisplayName = "Tasks should sort by due date then title.")]
    [Trait("Category", "Task Repository")]
    public async Task TaskRepository_GetByUserAsync_ShouldSortByDueDate()
    {
        // Arrange
        await _fixtures.SeedDatedTasksAsync();
        var query = new TaskQuery(null, null, null, null, null, null, 1, 100);

        // Act
        var page = await _fixtures.Repository.GetByUserAsync("user1", query);

        // Assert
        Assert.Equal(3, page.Items.Count);
        Assert.Equal("Early task", page.Items[0].Title);
        Assert.Equal("Late task", page.Items[1].Title);
        Assert.Equal("No due date", page.Items[2].Title);
    }

    [Fact(DisplayName = "Filtering by multiple statuses should return union.")]
    [Trait("Category", "Task Repository")]
    public async Task TaskRepository_GetByUserAsync_MultiStatus_ShouldReturnUnion()
    {
        // Arrange
        await _fixtures.SeedDefaultTasksAsync();
        var query = new TaskQuery(null, [KanbanStatus.Todo, KanbanStatus.Completed], null, null, null, null, 1, 100);

        // Act
        var page = await _fixtures.Repository.GetByUserAsync("user1", query);

        // Assert
        Assert.Equal(2, page.TotalCount);
    }

    [Fact(DisplayName = "Filtering by created date range should return only matching items.")]
    [Trait("Category", "Task Repository")]
    public async Task TaskRepository_GetByUserAsync_FilterByCreatedRange_ShouldReturnMatchingOnly()
    {
        // Arrange
        await _fixtures.SeedDateRangeTasksAsync();
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);
        var query = new TaskQuery(null, null, from, to, null, null, 1, 100);

        // Act
        var page = await _fixtures.Repository.GetByUserAsync("user1", query);

        // Assert
        Assert.Equal(2, page.TotalCount);
        Assert.All(page.Items, t => Assert.True(t.CreatedAt >= from && t.CreatedAt <= to));
    }

    [Fact(DisplayName = "Filtering by updated date range should return only matching items.")]
    [Trait("Category", "Task Repository")]
    public async Task TaskRepository_GetByUserAsync_FilterByUpdatedRange_ShouldReturnMatchingOnly()
    {
        // Arrange
        await _fixtures.SeedDateRangeTasksAsync();
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero);
        var query = new TaskQuery(null, null, null, null, from, to, 1, 100);

        // Act
        var page = await _fixtures.Repository.GetByUserAsync("user1", query);

        // Assert
        Assert.Equal(2, page.TotalCount);
        Assert.All(page.Items, t => Assert.True(t.UpdatedAt >= from && t.UpdatedAt <= to));
    }

    [Fact(DisplayName = "GetByIdAsync with missing id should return null.")]
    [Trait("Category", "Task Repository")]
    public async Task TaskRepository_GetByIdAsync_MissingId_ShouldReturnNull()
    {
        // Arrange
        await _fixtures.SeedDefaultTasksAsync();

        // Act
        var task = await _fixtures.Repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(task);
    }
}
