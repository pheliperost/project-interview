using BlaInterview.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BlaInterview.Tasks.Api.Controllers;

public abstract class BaseController : ControllerBase
{
    private readonly INotifyer _notifyer;

    protected BaseController(INotifyer notifyer)
    {
        _notifyer = notifyer;
    }

    protected bool ValidOperation()
    {
        return !_notifyer.HasNotification();
    }

    protected ActionResult NotificationError()
    {
        var notifications = _notifyer.GetNotifications();
        var message = string.Join(' ', notifications.Select(n => n.Message));
        var statusCode = notifications.Count > 0 ? notifications[0].StatusCode : StatusCodes.Status400BadRequest;
        return StatusCode(statusCode, new { error = message });
    }
}
