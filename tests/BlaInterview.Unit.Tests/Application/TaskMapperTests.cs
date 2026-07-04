using BlaInterview.Application.DTOs;
using BlaInterview.Application.Mapping;
using BlaInterview.Domain.Entities;
using BlaInterview.Domain.Enums;
using BlaInterview.Unit.Tests.Fixtures;

namespace BlaInterview.Unit.Tests.Application;

public class TaskMapperTests
{
    [Fact(DisplayName = "ToQuery should trim search and cap page size.")]
    [Trait("Category", "Task Mapper")]
    public void TaskMapper_ToQuery_ShouldMapFilterFields()
    {
        var filter = new TaskFilterRequest("  API  ", [KanbanStatus.Todo], null, null, null, null, 2, 500);
        var query = TaskMapper.ToQuery(filter);

        Assert.Equal("API", query.SearchTerm);
        Assert.Single(query.Statuses!);
        Assert.Equal(2, query.Page);
        Assert.Equal(TaskMapper.MaxPageSize, query.PageSize);
    }

    [Fact(DisplayName = "ToListResponse should map pagination metadata.")]
    [Trait("Category", "Task Mapper")]
    public void TaskMapper_ToListResponse_ShouldMapPagination()
    {
        var task = DomainFixtures.GenerateValidTaskItem("user1", KanbanStatus.Todo);
        var response = TaskMapper.ToListResponse([task], 10, 2, 5);

        Assert.Equal(10, response.TotalCount);
        Assert.Equal(2, response.Page);
        Assert.Equal(5, response.PageSize);
        Assert.Single(response.Items);
    }

    [Fact(DisplayName = "CreateEntity should default status to Todo.")]
    [Trait("Category", "Task Mapper")]
    public void TaskMapper_CreateEntity_ShouldDefaultTodo()
    {
        var now = DateTimeOffset.UtcNow;
        var request = new CreateTaskRequest("New task", "Desc", null, null);
        var entity = TaskMapper.CreateEntity("user1", request, now);

        Assert.Equal(KanbanStatus.Todo, entity.Status);
        Assert.Equal(TaskPriority.Medium, entity.Priority);
        Assert.Equal("user1", entity.UserId);
        Assert.Equal(now, entity.CreatedAt);
    }

    [Fact(DisplayName = "ToResponse should map all task fields.")]
    [Trait("Category", "Task Mapper")]
    public void TaskMapper_ToResponse_ShouldMapFields()
    {
        var task = DomainFixtures.GenerateValidTaskItem("user1", KanbanStatus.InProgress);
        var response = TaskMapper.ToResponse(task);

        Assert.Equal(task.Id, response.Id);
        Assert.Equal(task.Title, response.Title);
        Assert.Equal(task.Status, response.Status);
        Assert.Equal(task.Priority, response.Priority);
    }
}
