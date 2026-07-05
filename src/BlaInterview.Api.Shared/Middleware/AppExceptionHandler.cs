using BlaInterview.Application.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace BlaInterview.Api.Shared.Middleware;

public class AppExceptionHandler : IExceptionHandler
{
    private readonly ILogger<AppExceptionHandler> _logger;

    public AppExceptionHandler(ILogger<AppExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, message) = exception switch
        {
            AppException app => (app.StatusCode, app.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new { error = message }, cancellationToken);
        return true;
    }
}
