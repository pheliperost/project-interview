using BlaInterview.Domain.Enums;

namespace BlaInterview.Application.DTOs;

public record CreateTaskRequest(
    string Title,
    string? Description,
    TaskPriority? Priority,
    DateTimeOffset? DueDate);

public record UpdateTaskRequest(
    string Title,
    string? Description,
    KanbanStatus Status,
    TaskPriority Priority,
    DateTimeOffset? DueDate);

public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    KanbanStatus Status,
    TaskPriority Priority,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record TaskFilterRequest(
    string? Search,
    IReadOnlyList<KanbanStatus>? Statuses,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo,
    DateTimeOffset? UpdatedFrom,
    DateTimeOffset? UpdatedTo,
    int? Page,
    int? PageSize);

public record TaskListResponse(
    IReadOnlyList<TaskResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
