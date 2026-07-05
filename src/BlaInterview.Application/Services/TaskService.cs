using AutoMapper;
using BlaInterview.Application.DTOs;
using BlaInterview.Application.Interfaces;
using BlaInterview.Application.Mapping.Profiles;
using BlaInterview.Application.Queries;
using BlaInterview.Domain.Enums;
using FluentValidation;

namespace BlaInterview.Application.Services;

public class TaskService : BaseService, ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateTaskBody> _createValidator;
    private readonly IValidator<UpdateTaskBody> _updateValidator;
    private readonly IValidator<TaskFilterRequest> _filterValidator;

    public TaskService(
        ITaskRepository repository,
        IMapper mapper,
        INotifyer notifyer,
        IValidator<CreateTaskBody> createValidator,
        IValidator<UpdateTaskBody> updateValidator,
        IValidator<TaskFilterRequest> filterValidator)
        : base(notifyer)
    {
        _repository = repository;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _filterValidator = filterValidator;
    }

    public async Task<TaskListResponse?> GetTasksAsync(GetTasksRequest request, CancellationToken cancellationToken = default)
    {
        if (!await ValidateAsync(_filterValidator, request.Filter, cancellationToken))
        {
            return null;
        }

        var query = _mapper.Map<TaskQuery>(request.Filter);
        var page = await _repository.GetByUserAsync(request.UserId, query, cancellationToken);
        return new TaskListResponse(
            page.Items.Select(item => _mapper.Map<TaskResponse>(item)).ToList(),
            page.TotalCount,
            query.Page,
            query.PageSize);
    }

    public async Task<TaskResponse?> GetTaskByIdAsync(GetTaskByIdRequest request, CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedTaskAsync(request.UserId, request.TaskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        return _mapper.Map<TaskResponse>(task);
    }

    public async Task<TaskResponse?> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        if (!await ValidateAsync(_createValidator, request.Body, cancellationToken))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var task = _mapper.Map<Domain.Entities.TaskItem>(request.Body, opt =>
        {
            opt.Items[TaskProfile.UserIdContextKey] = request.UserId;
            opt.Items[TaskProfile.NowContextKey] = now;
        });
        await _repository.AddAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return _mapper.Map<TaskResponse>(task);
    }

    public async Task<TaskResponse?> UpdateTaskAsync(UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        if (!await ValidateAsync(_updateValidator, request.Body, cancellationToken))
        {
            return null;
        }

        var task = await GetOwnedTaskAsync(request.UserId, request.TaskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        if (task.IsTerminal && request.Body.Status != task.Status)
        {
            Notify("Terminal tasks cannot change status via update. Use reactivate.");
            return null;
        }

        if (IsDueDateChangedToPast(task.DueDate, request.Body.DueDate))
        {
            Notify("Due date cannot be in the past.");
            return null;
        }

        task.Title = request.Body.Title;
        task.Description = request.Body.Description;
        task.Priority = request.Body.Priority;
        task.DueDate = request.Body.DueDate;
        task.Status = request.Body.Status;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return _mapper.Map<TaskResponse>(task);
    }

    public async Task DeleteTaskAsync(DeleteTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedTaskAsync(request.UserId, request.TaskId, cancellationToken);
        if (task is null)
        {
            return;
        }

        await _repository.DeleteAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<TaskResponse?> ReactivateAsync(ReactivateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = await GetOwnedTaskAsync(request.UserId, request.TaskId, cancellationToken);
        if (task is null)
        {
            return null;
        }

        if (!task.IsTerminal)
        {
            Notify("Only completed or cancelled tasks can be reactivated.");
            return null;
        }

        task.Status = KanbanStatus.Todo;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.UpdateAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return _mapper.Map<TaskResponse>(task);
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

    private static bool IsDueDateChangedToPast(DateTimeOffset? current, DateTimeOffset? requested)
    {
        if (requested is null || DueDatesEqual(current, requested))
        {
            return false;
        }

        return requested.Value.UtcDateTime.Date < DateTimeOffset.UtcNow.Date;
    }

    private static bool DueDatesEqual(DateTimeOffset? left, DateTimeOffset? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Value.UtcDateTime.Date == right.Value.UtcDateTime.Date;
    }
}
