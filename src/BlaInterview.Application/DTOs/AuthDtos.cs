namespace BlaInterview.Application.DTOs;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record AuthResponse(string Token, DateTimeOffset ExpiresAt, string Email);

/// <summary>
/// Demo-only: <see cref="ResetLink"/> is populated when the account exists so the UI can show a reset card without real email.
/// </summary>
public record ForgotPasswordResponse(string Message, string? ResetLink);

public record ResetPasswordResponse(string Message);
