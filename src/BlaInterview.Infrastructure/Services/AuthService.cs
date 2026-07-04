using BlaInterview.Application.Common;
using BlaInterview.Application.DTOs;
using BlaInterview.Application.Interfaces;
using BlaInterview.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace BlaInterview.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_registerValidator, request, cancellationToken);

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            throw new AppException("Email is already registered.", 409);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new AppException(string.Join(' ', result.Errors.Select(e => e.Description)), 400);

        return _jwtTokenService.CreateToken(user.Id, user.Email!);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_loginValidator, request, cancellationToken);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            throw new AppException("Invalid email or password.", 401);

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            throw new AppException("Invalid email or password.", 401);

        return _jwtTokenService.CreateToken(user.Id, user.Email!);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
            throw new AppException(string.Join(' ', result.Errors.Select(e => e.ErrorMessage)), 400);
    }
}
