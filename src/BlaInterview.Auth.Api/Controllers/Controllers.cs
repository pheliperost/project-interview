using BlaInterview.Application.DTOs;
using BlaInterview.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlaInterview.Auth.Api.Controllers;

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

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _authService.ForgotPasswordAsync(request, cancellationToken));

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _authService.ResetPasswordAsync(request, cancellationToken));
}

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/api/health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { status = "healthy" });
}
