using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BlaInterview.Api.Models;
using BlaInterview.Application.DTOs;
using BlaInterview.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlaInterview.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken) =>
        Ok(await _authService.RegisterAsync(request, cancellationToken));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await _authService.LoginAsync(request, cancellationToken));

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout() => NoContent();
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<TaskListResponse>> GetTasks(
        [FromQuery] TaskListQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _taskService.GetTasksAsync(UserId, query.ToFilterRequest(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskResponse>> GetTask(Guid id, CancellationToken cancellationToken) =>
        Ok(await _taskService.GetTaskByIdAsync(UserId, id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<TaskResponse>> CreateTask(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await _taskService.CreateTaskAsync(UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskResponse>> UpdateTask(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken) =>
        Ok(await _taskService.UpdateTaskAsync(UserId, id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken cancellationToken)
    {
        await _taskService.DeleteTaskAsync(UserId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<ActionResult<TaskResponse>> Reactivate(Guid id, CancellationToken cancellationToken) =>
        Ok(await _taskService.ReactivateAsync(UserId, id, cancellationToken));

    private string UserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? throw new UnauthorizedAccessException();
}

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/api/health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { status = "healthy" });
}
