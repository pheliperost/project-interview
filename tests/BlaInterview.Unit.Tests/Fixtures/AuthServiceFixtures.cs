using BlaInterview.Application.DTOs;
using BlaInterview.Application.Interfaces;
using BlaInterview.Application.Validators;
using BlaInterview.Infrastructure.Identity;
using BlaInterview.Infrastructure.Options;
using BlaInterview.Infrastructure.Services;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;

namespace BlaInterview.Unit.Tests.Fixtures;

[CollectionDefinition(nameof(AuthServiceCollection))]
public class AuthServiceCollection : ICollectionFixture<AuthServiceFixtures>
{
}

public class AuthServiceFixtures
{
    public Mock<UserManager<ApplicationUser>> UserManager { get; private set; } = null!;
    public Mock<SignInManager<ApplicationUser>> SignInManager { get; private set; } = null!;
    public Mock<IJwtTokenService> JwtTokenService { get; private set; } = null!;
    public Mock<IEmailSender> EmailSender { get; private set; } = null!;
    public Mock<IValidator<RegisterRequest>> RegisterValidator { get; private set; } = null!;
    public Mock<IValidator<LoginRequest>> LoginValidator { get; private set; } = null!;
    public Mock<IValidator<ForgotPasswordRequest>> ForgotPasswordValidator { get; private set; } = null!;
    public Mock<IValidator<ResetPasswordRequest>> ResetPasswordValidator { get; private set; } = null!;

    public AuthService CreateService()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        UserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        SignInManager = new Mock<SignInManager<ApplicationUser>>(
            UserManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null!, null!, null!, null!);

        JwtTokenService = new Mock<IJwtTokenService>();
        JwtTokenService
            .Setup(j => j.CreateToken(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string userId, string email) =>
                new AuthResponse("test-token", DateTimeOffset.UtcNow.AddHours(1), email));

        EmailSender = new Mock<IEmailSender>();
        EmailSender
            .Setup(e => e.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        RegisterValidator = new Mock<IValidator<RegisterRequest>>();
        RegisterValidator
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        LoginValidator = new Mock<IValidator<LoginRequest>>();
        LoginValidator
            .Setup(v => v.ValidateAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        ForgotPasswordValidator = new Mock<IValidator<ForgotPasswordRequest>>();
        ForgotPasswordValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ForgotPasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        ResetPasswordValidator = new Mock<IValidator<ResetPasswordRequest>>();
        ResetPasswordValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ResetPasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        return new AuthService(
            UserManager.Object,
            SignInManager.Object,
            JwtTokenService.Object,
            EmailSender.Object,
            Options.Create(new PasswordResetOptions { ClientBaseUrl = "http://localhost:5173" }),
            RegisterValidator.Object,
            LoginValidator.Object,
            ForgotPasswordValidator.Object,
            ResetPasswordValidator.Object);
    }

    public AuthService CreateServiceWithRealValidators()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        UserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        SignInManager = new Mock<SignInManager<ApplicationUser>>(
            UserManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null!, null!, null!, null!);

        JwtTokenService = new Mock<IJwtTokenService>();
        EmailSender = new Mock<IEmailSender>();

        return new AuthService(
            UserManager.Object,
            SignInManager.Object,
            JwtTokenService.Object,
            EmailSender.Object,
            Options.Create(new PasswordResetOptions { ClientBaseUrl = "http://localhost:5173" }),
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new ForgotPasswordRequestValidator(),
            new ResetPasswordRequestValidator());
    }

    public static ApplicationUser CreateUser(string email = "user@example.local") =>
        new()
        {
            Id = "user-1",
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
}
