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
}
