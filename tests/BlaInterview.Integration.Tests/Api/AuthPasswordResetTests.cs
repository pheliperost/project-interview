using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlaInterview.Application.DTOs;
using BlaInterview.Infrastructure.Services;
using BlaInterview.Integration.Tests.Config;
using Microsoft.Extensions.DependencyInjection;

namespace BlaInterview.Integration.Tests.Api;

[Collection(nameof(IntegrationWebTestsFixtureCollection))]
public class AuthPasswordResetTests
{
    private readonly IntegrationTestsFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthPasswordResetTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "Forgot password for existing user should return demo reset link.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_ForgotPassword_ExistingUser_ShouldReturnResetLink()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();
        var emailSender = GetEmailSender();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest(IntegrationTestsFixture.DemoEmail));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>(JsonOptions);
        Assert.NotNull(body?.ResetLink);
        Assert.Contains("/reset-password", body.ResetLink, StringComparison.Ordinal);
        Assert.Equal(IntegrationTestsFixture.DemoEmail, emailSender.LastEmail);
        Assert.Equal(body.ResetLink, emailSender.LastResetLink);
    }

    [Fact(DisplayName = "Forgot password for unknown email should not return reset link.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_ForgotPassword_UnknownEmail_ShouldNotReturnResetLink()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();
        var emailSender = GetEmailSender();
        var beforeCount = emailSender.SendCount;

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest("unknown@example.local"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ForgotPasswordResponse>(JsonOptions);
        Assert.Null(body?.ResetLink);
        Assert.Equal(beforeCount, emailSender.SendCount);
    }

    [Fact(DisplayName = "Forgot password with invalid email should return 400.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_ForgotPassword_InvalidEmail_ShouldReturn400()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest("not-an-email"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "Full password reset flow should allow login with new password.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_ResetPassword_FullFlow_ShouldAllowLoginWithNewPassword()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();
        var email = $"reset-{Guid.NewGuid():N}@example.local";
        const string oldPassword = "OldPass1!";
        const string newPassword = "NewPass2!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, oldPassword));
        registerResponse.EnsureSuccessStatusCode();

        var forgotResponse = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest(email));
        forgotResponse.EnsureSuccessStatusCode();
        var forgot = await forgotResponse.Content.ReadFromJsonAsync<ForgotPasswordResponse>(JsonOptions);
        Assert.NotNull(forgot?.ResetLink);

        var resetUri = new Uri(forgot.ResetLink);
        var token = ParseQueryParam(resetUri, "token");
        Assert.False(string.IsNullOrWhiteSpace(token));

        // Act
        var resetResponse = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordRequest(email, token!, newPassword));

        // Assert
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        var oldLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, oldPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, newPassword));
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact(DisplayName = "Reset password with invalid token should return 400.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_ResetPassword_InvalidToken_ShouldReturn400()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordRequest(IntegrationTestsFixture.DemoEmail, "invalid-token", "NewPass1!"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "Reset password with weak password should return 400.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_ResetPassword_WeakPassword_ShouldReturn400()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordRequest(IntegrationTestsFixture.DemoEmail, "some-token", "short"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private FakeEmailSender GetEmailSender()
    {
        return _fixture.AuthFactory.Services.GetRequiredService<FakeEmailSender>();
    }

    private static string? ParseQueryParam(Uri uri, string name)
    {
        var query = uri.Query.TrimStart('?');
        if (string.IsNullOrEmpty(query))
            return null;

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && pair[0] == name)
                return Uri.UnescapeDataString(pair[1]);
        }

        return null;
    }
}
