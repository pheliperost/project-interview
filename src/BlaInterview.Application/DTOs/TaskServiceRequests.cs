namespace BlaInterview.Application.DTOs;

public record GetTasksRequest(string UserId, TaskFilterRequest Filter);

public record GetTaskByIdRequest(string UserId, Guid TaskId);

public record CreateTaskRequest(string UserId, CreateTaskBody Body);

public record UpdateTaskRequest(string UserId, Guid TaskId, UpdateTaskBody Body);

public record DeleteTaskRequest(string UserId, Guid TaskId);

public record ReactivateTaskRequest(string UserId, Guid TaskId);
