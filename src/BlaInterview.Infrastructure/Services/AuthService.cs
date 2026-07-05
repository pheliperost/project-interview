using BlaInterview.Application.Common;
using BlaInterview.Application.DTOs;
using BlaInterview.Application.Interfaces;
using BlaInterview.Infrastructure.Identity;
using BlaInterview.Infrastructure.Options;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BlaInterview.Infrastructure.Services;

public class AuthService : IAuthService
{
    private const string ForgotPasswordMessage =
        "If an account exists for this email, password reset instructions have been sent.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailSender _emailSender;
    private readonly PasswordResetOptions _passwordResetOptions;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        IEmailSender emailSender,
        IOptions<PasswordResetOptions> passwordResetOptions,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _emailSender = emailSender;
        _passwordResetOptions = passwordResetOptions.Value;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_registerValidator, request, cancellationToken);

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new AppException("Email is already registered.", 409);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new AppException(string.Join(' ', result.Errors.Select(e => e.Description)), 400);
        }

        return _jwtTokenService.CreateToken(user.Id, user.Email!);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_loginValidator, request, cancellationToken);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            throw new AppException("Invalid email or password.", 401);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            throw new AppException("Invalid email or password.", 401);
        }

        return _jwtTokenService.CreateToken(user.Id, user.Email!);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_forgotPasswordValidator, request, cancellationToken);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return new ForgotPasswordResponse(ForgotPasswordMessage, ResetLink: null);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = BuildResetLink(user.Email!, token);
        await _emailSender.SendPasswordResetAsync(user.Email!, resetLink, cancellationToken);

        return new ForgotPasswordResponse(ForgotPasswordMessage, resetLink);
    }

    public async Task<ResetPasswordResponse> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_resetPasswordValidator, request, cancellationToken);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            throw new AppException("Invalid or expired reset token.", 400);
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new AppException(string.Join(' ', result.Errors.Select(e => e.Description)), 400);
        }

        return new ResetPasswordResponse("Password has been reset. You can sign in with your new password.");
    }

    private string BuildResetLink(string email, string token)
    {
        var baseUrl = _passwordResetOptions.ClientBaseUrl.TrimEnd('/');
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedToken = Uri.EscapeDataString(token);
        return $"{baseUrl}/reset-password?email={encodedEmail}&token={encodedToken}";
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new AppException(string.Join(' ', result.Errors.Select(e => e.ErrorMessage)), 400);
        }
    }
}
