using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BlaInterview.Application.DTOs;
using BlaInterview.Application.Interfaces;
using BlaInterview.Tasks.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlaInterview.Tasks.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : BaseController
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService, INotifyer notifyer)
        : base(notifyer)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<TaskListResponse>> GetTasks(
        [FromQuery] TaskListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetTasksAsync(
            new GetTasksRequest(UserId, query.ToFilterRequest()),
            cancellationToken);
        if (!ValidOperation())
            return NotificationError();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskResponse>> GetTask(Guid id, CancellationToken cancellationToken)
    {
        var result = await _taskService.GetTaskByIdAsync(new GetTaskByIdRequest(UserId, id), cancellationToken);
        if (!ValidOperation())
            return NotificationError();

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponse>> CreateTask(CreateTaskBody body, CancellationToken cancellationToken)
    {
        var task = await _taskService.CreateTaskAsync(new CreateTaskRequest(UserId, body), cancellationToken);
        if (!ValidOperation())
            return NotificationError();

        return CreatedAtAction(nameof(GetTask), new { id = task!.Id }, task);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskResponse>> UpdateTask(Guid id, UpdateTaskBody body, CancellationToken cancellationToken)
    {
        var result = await _taskService.UpdateTaskAsync(new UpdateTaskRequest(UserId, id, body), cancellationToken);
        if (!ValidOperation())
            return NotificationError();

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken cancellationToken)
    {
        await _taskService.DeleteTaskAsync(new DeleteTaskRequest(UserId, id), cancellationToken);
        if (!ValidOperation())
            return NotificationError();

        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<ActionResult<TaskResponse>> Reactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _taskService.ReactivateAsync(new ReactivateTaskRequest(UserId, id), cancellationToken);
        if (!ValidOperation())
            return NotificationError();

        return Ok(result);
    }

    private string UserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? throw new UnauthorizedAccessException();
}
