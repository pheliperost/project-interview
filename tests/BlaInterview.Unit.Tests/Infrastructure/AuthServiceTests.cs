using BlaInterview.Application.Common;
using BlaInterview.Application.DTOs;
using BlaInterview.Infrastructure.Identity;
using BlaInterview.Unit.Tests.Fixtures;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace BlaInterview.Unit.Tests.Infrastructure;

[Collection(nameof(AuthServiceCollection))]
public class AuthServiceTests
{
    private readonly AuthServiceFixtures _fixtures;

    public AuthServiceTests(AuthServiceFixtures fixtures)
    {
        _fixtures = fixtures;
    }

    [Fact(DisplayName = "Register with valid request should return a JWT.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_Register_ValidRequest_ShouldReturnToken()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new RegisterRequest("new@example.local", "Test1234!");
        _fixtures.UserManager
            .Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);
        _fixtures.UserManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var response = await service.RegisterAsync(request);

        // Assert
        Assert.Equal("test-token", response.Token);
        _fixtures.JwtTokenService.Verify(j => j.CreateToken(It.IsAny<string>(), request.Email), Times.Once);
    }

    [Fact(DisplayName = "Register with duplicate email should throw 409.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_Register_DuplicateEmail_ShouldThrow409()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new RegisterRequest("existing@example.local", "Test1234!");
        _fixtures.UserManager
            .Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync(AuthServiceFixtures.CreateUser(request.Email));

        // Act
        var exception = await Assert.ThrowsAsync<AppException>(() => service.RegisterAsync(request));

        // Assert
        Assert.Equal(409, exception.StatusCode);
        _fixtures.UserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "Register when Identity create fails should throw 400.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_Register_IdentityCreateFails_ShouldThrow400()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new RegisterRequest("new@example.local", "Test1234!");
        _fixtures.UserManager
            .Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);
        _fixtures.UserManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

        // Act
        var exception = await Assert.ThrowsAsync<AppException>(() => service.RegisterAsync(request));

        // Assert
        Assert.Equal(400, exception.StatusCode);
        Assert.Contains("Password too weak", exception.Message);
    }

    [Fact(DisplayName = "Register with invalid payload should throw 400.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_Register_InvalidPayload_ShouldThrow400()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new RegisterRequest("not-an-email", "short");
        _fixtures.RegisterValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Email", "Invalid email.")]));

        // Act
        var exception = await Assert.ThrowsAsync<AppException>(() => service.RegisterAsync(request));

        // Assert
        Assert.Equal(400, exception.StatusCode);
        _fixtures.UserManager.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "Login with valid credentials should return a JWT.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_Login_ValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new LoginRequest("user@example.local", "Demo123!");
        var user = AuthServiceFixtures.CreateUser(request.Email);
        _fixtures.UserManager
            .Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _fixtures.SignInManager
            .Setup(m => m.CheckPasswordSignInAsync(user, request.Password, true))
            .ReturnsAsync(SignInResult.Success);

        // Act
        var response = await service.LoginAsync(request);

        // Assert
        Assert.Equal("test-token", response.Token);
        _fixtures.JwtTokenService.Verify(j => j.CreateToken(user.Id, request.Email), Times.Once);
    }

    [Fact(DisplayName = "Login with unknown email should throw 401.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_Login_UnknownEmail_ShouldThrow401()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new LoginRequest("unknown@example.local", "Demo123!");
        _fixtures.UserManager
            .Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var exception = await Assert.ThrowsAsync<AppException>(() => service.LoginAsync(request));

        // Assert
        Assert.Equal(401, exception.StatusCode);
    }

    [Fact(DisplayName = "Login with wrong password should throw 401.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_Login_WrongPassword_ShouldThrow401()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new LoginRequest("user@example.local", "WrongPass1!");
        var user = AuthServiceFixtures.CreateUser(request.Email);
        _fixtures.UserManager
            .Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _fixtures.SignInManager
            .Setup(m => m.CheckPasswordSignInAsync(user, request.Password, true))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var exception = await Assert.ThrowsAsync<AppException>(() => service.LoginAsync(request));

        // Assert
        Assert.Equal(401, exception.StatusCode);
    }

    [Fact(DisplayName = "Login with invalid payload should throw 400.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_Login_InvalidPayload_ShouldThrow400()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new LoginRequest("not-an-email", "");
        _fixtures.LoginValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Password", "Password is required.")]));

        // Act
        var exception = await Assert.ThrowsAsync<AppException>(() => service.LoginAsync(request));

        // Assert
        Assert.Equal(400, exception.StatusCode);
        _fixtures.UserManager.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "Forgot password with existing email should return demo reset link and send fake email.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_ForgotPassword_ExistingEmail_ShouldReturnResetLink()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new ForgotPasswordRequest("user@example.local");
        var user = AuthServiceFixtures.CreateUser(request.Email);
        _fixtures.UserManager
            .Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _fixtures.UserManager
            .Setup(m => m.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("reset-token-123");

        // Act
        var response = await service.ForgotPasswordAsync(request);

        // Assert
        Assert.NotNull(response.ResetLink);
        Assert.Contains("reset-password", response.ResetLink, StringComparison.Ordinal);
        Assert.Contains(Uri.EscapeDataString(request.Email), response.ResetLink, StringComparison.Ordinal);
        _fixtures.EmailSender.Verify(
            e => e.SendPasswordResetAsync(request.Email, response.ResetLink!, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Forgot password with unknown email should not return reset link.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_ForgotPassword_UnknownEmail_ShouldNotReturnResetLink()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new ForgotPasswordRequest("unknown@example.local");
        _fixtures.UserManager
            .Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var response = await service.ForgotPasswordAsync(request);

        // Assert
        Assert.Null(response.ResetLink);
        _fixtures.EmailSender.Verify(
            e => e.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Reset password with valid token should succeed.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_ResetPassword_ValidToken_ShouldSucceed()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new ResetPasswordRequest("user@example.local", "valid-token", "NewPass1!");
        var user = AuthServiceFixtures.CreateUser(request.Email);
        _fixtures.UserManager
            .Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _fixtures.UserManager
            .Setup(m => m.ResetPasswordAsync(user, request.Token, request.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var response = await service.ResetPasswordAsync(request);

        // Assert
        Assert.Contains("reset", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Reset password with invalid token should throw 400.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_ResetPassword_InvalidToken_ShouldThrow400()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new ResetPasswordRequest("user@example.local", "bad-token", "NewPass1!");
        var user = AuthServiceFixtures.CreateUser(request.Email);
        _fixtures.UserManager
            .Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);
        _fixtures.UserManager
            .Setup(m => m.ResetPasswordAsync(user, request.Token, request.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        // Act
        var exception = await Assert.ThrowsAsync<AppException>(() => service.ResetPasswordAsync(request));

        // Assert
        Assert.Equal(400, exception.StatusCode);
    }

    [Fact(DisplayName = "Reset password with invalid payload should throw 400.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_ResetPassword_InvalidPayload_ShouldThrow400()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new ResetPasswordRequest("not-an-email", "", "short");
        _fixtures.ResetPasswordValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Token", "Token is required.")]));

        // Act
        var exception = await Assert.ThrowsAsync<AppException>(() => service.ResetPasswordAsync(request));

        // Assert
        Assert.Equal(400, exception.StatusCode);
        _fixtures.UserManager.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "Forgot password with invalid payload should throw 400.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_ForgotPassword_InvalidPayload_ShouldThrow400()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new ForgotPasswordRequest("not-an-email");
        _fixtures.ForgotPasswordValidator
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure("Email", "Invalid email.")]));

        // Act
        var exception = await Assert.ThrowsAsync<AppException>(() => service.ForgotPasswordAsync(request));

        // Assert
        Assert.Equal(400, exception.StatusCode);
        _fixtures.UserManager.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact(DisplayName = "Reset password for unknown user should throw 400.")]
    [Trait("Category", "Auth Service")]
    public async Task AuthService_ResetPassword_UnknownUser_ShouldThrow400()
    {
        // Arrange
        var service = _fixtures.CreateService();
        var request = new ResetPasswordRequest("unknown@example.local", "token", "NewPass1!");
        _fixtures.UserManager
            .Setup(m => m.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var exception = await Assert.ThrowsAsync<AppException>(() => service.ResetPasswordAsync(request));

        // Assert
        Assert.Equal(400, exception.StatusCode);
        _fixtures.UserManager.Verify(
            m => m.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}
