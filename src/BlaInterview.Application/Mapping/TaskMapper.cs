using BlaInterview.Application.DTOs;
using BlaInterview.Application.Queries;
using BlaInterview.Domain.Entities;
using BlaInterview.Domain.Enums;

namespace BlaInterview.Application.Mapping;

public static class TaskMapper
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 100;
    public const int MaxPageSize = 200;

    public static TaskQuery ToQuery(TaskFilterRequest filter)
    {
        var page = filter.Page is > 0 ? filter.Page.Value : DefaultPage;
        var pageSize = filter.PageSize is > 0
            ? Math.Min(filter.PageSize.Value, MaxPageSize)
            : DefaultPageSize;

        return new TaskQuery(
            string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim(),
            filter.Statuses,
            filter.CreatedFrom,
            filter.CreatedTo,
            filter.UpdatedFrom,
            filter.UpdatedTo,
            page,
            pageSize);
    }

    public static TaskListResponse ToListResponse(IReadOnlyList<TaskItem> items, int totalCount, int page, int pageSize) =>
        new(items.Select(ToResponse).ToList(), totalCount, page, pageSize);

    public static TaskResponse ToResponse(TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.Priority,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt);

    public static TaskItem CreateEntity(string userId, CreateTaskRequest request, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        Title = request.Title,
        Description = request.Description,
        Status = KanbanStatus.Todo,
        Priority = request.Priority ?? TaskPriority.Medium,
        DueDate = request.DueDate,
        UserId = userId,
        CreatedAt = now,
        UpdatedAt = now
    };
}
