using BlaInterview.Application.DTOs;
using BlaInterview.Application.Queries;
using BlaInterview.Domain.Entities;

namespace BlaInterview.Application.Interfaces;

public interface ITaskRepository
{
    Task<TaskPage> GetByUserAsync(string userId, TaskQuery query, CancellationToken cancellationToken = default);
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task DeleteAsync(TaskItem task, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ITaskService
{
    Task<TaskListResponse?> GetTasksAsync(GetTasksRequest request, CancellationToken cancellationToken = default);
    Task<TaskResponse?> GetTaskByIdAsync(GetTaskByIdRequest request, CancellationToken cancellationToken = default);
    Task<TaskResponse?> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<TaskResponse?> UpdateTaskAsync(UpdateTaskRequest request, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(DeleteTaskRequest request, CancellationToken cancellationToken = default);
    Task<TaskResponse?> ReactivateAsync(ReactivateTaskRequest request, CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}

public interface IJwtTokenService
{
    AuthResponse CreateToken(string userId, string email);
}
