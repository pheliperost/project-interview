using BlaInterview.Application.DTOs;
using BlaInterview.Application.Interfaces;
using BlaInterview.Application.Mapping;
using BlaInterview.Domain.Enums;
using FluentValidation;

namespace BlaInterview.Application.Services;

public class TaskService : BaseService, ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;
    private readonly IValidator<TaskFilterRequest> _filterValidator;

    public TaskService(
        ITaskRepository repository,
        INotifyer notifyer,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator,
        IValidator<TaskFilterRequest> filterValidator)
        : base(notifyer)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _filterValidator = filterValidator;
    }

    public async Task<TaskListResponse?> GetTasksAsync(string userId, TaskFilterRequest filter, CancellationToken cancellationToken = default)
    {
        if (!await ValidateAsync(_filterValidator, filter, cancellationToken))
            return null;

        var query = TaskMapper.ToQuery(filter);
        var page = await _repository.GetByUserAsync(userId, query, cancellationToken);
        return TaskMapper.ToListResponse(page.Items, page.TotalCount, query.Page, query.PageSize);
    }

    public async Task<TaskResponse?> GetTaskByIdAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedTaskAsync(userId, id, cancellationToken);
        if (task is null)
            return null;

        return TaskMapper.ToResponse(task);
    }

    public async Task<TaskResponse?> CreateTaskAsync(string userId, CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        if (!await ValidateAsync(_createValidator, request, cancellationToken))
            return null;

        var now = DateTimeOffset.UtcNow;
        var task = TaskMapper.CreateEntity(userId, request, now);
        await _repository.AddAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return TaskMapper.ToResponse(task);
    }

    public async Task<TaskResponse?> UpdateTaskAsync(string userId, Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        if (!await ValidateAsync(_updateValidator, request, cancellationToken))
            return null;

        var task = await GetOwnedTaskAsync(userId, id, cancellationToken);
        if (task is null)
            return null;

        if (task.IsTerminal && request.Status != task.Status)
        {
            Notify("Terminal tasks cannot change status via update. Use reactivate.");
            return null;
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.Status = request.Status;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return TaskMapper.ToResponse(task);
    }

    public async Task DeleteTaskAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedTaskAsync(userId, id, cancellationToken);
        if (task is null)
            return;

        await _repository.DeleteAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<TaskResponse?> ReactivateAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedTaskAsync(userId, id, cancellationToken);
        if (task is null)
            return null;

        if (!task.IsTerminal)
        {
            Notify("Only completed or cancelled tasks can be reactivated.");
            return null;
        }

        task.Status = KanbanStatus.Todo;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.UpdateAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return TaskMapper.ToResponse(task);
    }

    private async Task<Domain.Entities.TaskItem?> GetOwnedTaskAsync(string userId, Guid id, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            Notify("Task not found.", 404);
            return null;
        }

        if (task.UserId != userId)
        {
            Notify("You do not have access to this task.", 403);
            return null;
        }

        return task;
    }
}
