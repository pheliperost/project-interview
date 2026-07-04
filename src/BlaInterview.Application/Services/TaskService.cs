using BlaInterview.Application.Common;
using BlaInterview.Application.DTOs;
using BlaInterview.Application.Interfaces;
using BlaInterview.Application.Mapping;
using BlaInterview.Domain.Enums;
using FluentValidation;

namespace BlaInterview.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;
    private readonly IValidator<TaskFilterRequest> _filterValidator;

    public TaskService(
        ITaskRepository repository,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator,
        IValidator<TaskFilterRequest> filterValidator)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _filterValidator = filterValidator;
    }

    public async Task<TaskListResponse> GetTasksAsync(string userId, TaskFilterRequest filter, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_filterValidator, filter, cancellationToken);
        var query = TaskMapper.ToQuery(filter);
        var page = await _repository.GetByUserAsync(userId, query, cancellationToken);
        return TaskMapper.ToListResponse(page.Items, page.TotalCount, query.Page, query.PageSize);
    }

    public async Task<TaskResponse> GetTaskByIdAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedTaskAsync(userId, id, cancellationToken);
        return TaskMapper.ToResponse(task);
    }

    public async Task<TaskResponse> CreateTaskAsync(string userId, CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var task = TaskMapper.CreateEntity(userId, request, now);
        await _repository.AddAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return TaskMapper.ToResponse(task);
    }

    public async Task<TaskResponse> UpdateTaskAsync(string userId, Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);
        var task = await GetOwnedTaskAsync(userId, id, cancellationToken);

        if (task.IsTerminal && request.Status != task.Status)
            throw new AppException("Terminal tasks cannot change status via update. Use reactivate.", 400);

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
        await _repository.DeleteAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<TaskResponse> ReactivateAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedTaskAsync(userId, id, cancellationToken);

        if (!task.IsTerminal)
            throw new AppException("Only completed or cancelled tasks can be reactivated.", 400);

        task.Status = KanbanStatus.Todo;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.UpdateAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return TaskMapper.ToResponse(task);
    }

    private async Task<Domain.Entities.TaskItem> GetOwnedTaskAsync(string userId, Guid id, CancellationToken cancellationToken)
    {
        var task = await _repository.GetByIdAsync(id, cancellationToken);
        if (task is null)
            throw new AppException("Task not found.", 404);

        if (task.UserId != userId)
            throw new AppException("You do not have access to this task.", 403);

        return task;
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
            throw new AppException(string.Join(' ', result.Errors.Select(e => e.ErrorMessage)), 400);
    }
}
