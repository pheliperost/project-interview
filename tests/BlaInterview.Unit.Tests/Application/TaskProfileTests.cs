using BlaInterview.Application.DTOs;
using BlaInterview.Application.Mapping;
using BlaInterview.Application.Mapping.Profiles;
using BlaInterview.Application.Queries;
using BlaInterview.Domain.Entities;
using BlaInterview.Domain.Enums;
using BlaInterview.Unit.Tests.Fixtures;

namespace BlaInterview.Unit.Tests.Application;

[Collection(nameof(MapperCollection))]
public class TaskProfileTests
{
    private readonly MapperFixtures _fixtures;

    public TaskProfileTests(MapperFixtures fixtures) => _fixtures = fixtures;

    [Fact(DisplayName = "TaskFilterRequest should trim search and cap page size.")]
    [Trait("Category", "Task Mapper")]
    public void TaskProfile_ToQuery_ShouldMapFilterFields()
    {
        var filter = new TaskFilterRequest("  API  ", [KanbanStatus.Todo], null, null, null, null, 2, 500);
        var query = _fixtures.Mapper.Map<TaskQuery>(filter);

        Assert.Equal("API", query.SearchTerm);
        Assert.Single(query.Statuses!);
        Assert.Equal(2, query.Page);
        Assert.Equal(TaskPagination.MaxPageSize, query.PageSize);
    }

    [Fact(DisplayName = "TaskItem list should map pagination metadata.")]
    [Trait("Category", "Task Mapper")]
    public void TaskProfile_ToListResponse_ShouldMapPagination()
    {
        var task = DomainFixtures.GenerateValidTaskItem("user1", KanbanStatus.Todo);
        var response = new TaskListResponse(
            [_fixtures.Mapper.Map<TaskResponse>(task)],
            10,
            2,
            5);

        Assert.Equal(10, response.TotalCount);
        Assert.Equal(2, response.Page);
        Assert.Equal(5, response.PageSize);
        Assert.Single(response.Items);
    }

    [Fact(DisplayName = "CreateTaskBody should default status to Todo.")]
    [Trait("Category", "Task Mapper")]
    public void TaskProfile_CreateEntity_ShouldDefaultTodo()
    {
        var now = DateTimeOffset.UtcNow;
        var request = new CreateTaskBody("New task", "Desc", null, null);
        var entity = _fixtures.Mapper.Map<TaskItem>(request, opt =>
        {
            opt.Items[TaskProfile.UserIdContextKey] = "user1";
            opt.Items[TaskProfile.NowContextKey] = now;
        });

        Assert.Equal(KanbanStatus.Todo, entity.Status);
        Assert.Equal(TaskPriority.Medium, entity.Priority);
        Assert.Equal("user1", entity.UserId);
        Assert.Equal(now, entity.CreatedAt);
    }

    [Fact(DisplayName = "TaskItem should map all task fields.")]
    [Trait("Category", "Task Mapper")]
    public void TaskProfile_ToResponse_ShouldMapFields()
    {
        var task = DomainFixtures.GenerateValidTaskItem("user1", KanbanStatus.InProgress);
        var response = _fixtures.Mapper.Map<TaskResponse>(task);

        Assert.Equal(task.Id, response.Id);
        Assert.Equal(task.Title, response.Title);
        Assert.Equal(task.Status, response.Status);
        Assert.Equal(task.Priority, response.Priority);
    }

    [Fact(DisplayName = "Empty filter should map default pagination.")]
    [Trait("Category", "Task Mapper")]
    public void TaskProfile_ToQuery_EmptyFilter_ShouldUseDefaults()
    {
        var filter = new TaskFilterRequest(null, null, null, null, null, null, null, null);
        var query = _fixtures.Mapper.Map<TaskQuery>(filter);

        Assert.Null(query.SearchTerm);
        Assert.Equal(TaskPagination.DefaultPage, query.Page);
        Assert.Equal(TaskPagination.DefaultPageSize, query.PageSize);
    }

    [Fact(DisplayName = "Whitespace search should trim to empty and map null.")]
    [Trait("Category", "Task Mapper")]
    public void TaskProfile_ToQuery_WhitespaceSearch_ShouldMapNull()
    {
        var filter = new TaskFilterRequest("   ", null, null, null, null, null, null, null);
        var query = _fixtures.Mapper.Map<TaskQuery>(filter);

        Assert.Null(query.SearchTerm);
    }
}
