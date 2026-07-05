using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlaInterview.Application.DTOs;
using BlaInterview.Integration.Tests.Config;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BlaInterview.Integration.Tests.Api;

[Collection(nameof(IntegrationWebTestsFixtureCollection))]
public class AuthExtendedTests
{
    private readonly IntegrationTestsFixture _fixture;

    public AuthExtendedTests(IntegrationTestsFixture fixture) => _fixture = fixture;

    [Fact(DisplayName = "Registering a new user should return a JWT.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_Register_NewUser_ShouldReturnToken()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();
        var email = $"test-{Guid.NewGuid():N}@example.local";

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Test1234!"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(IntegrationTestsFixture.JsonOptions);
        Assert.False(string.IsNullOrWhiteSpace(auth?.Token));
    }

    [Fact(DisplayName = "Logging in with wrong password should return 401.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_Login_WrongPassword_ShouldReturn401()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(IntegrationTestsFixture.DemoEmail, "WrongPass1!"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "Registering duplicate email should return 409.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_Register_DuplicateEmail_ShouldReturn409()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(IntegrationTestsFixture.DemoEmail, "Test1234!"));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact(DisplayName = "Registering with weak password should return 400.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_Register_WeakPassword_ShouldReturn400()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"weak-{Guid.NewGuid():N}@example.local", "short"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "Logging in with unknown email should return 401.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_Login_UnknownEmail_ShouldReturn401()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("unknown@example.local", "Demo123!"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "Logging out with valid token should return 204.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_Logout_WithToken_ShouldReturnNoContent()
    {
        // Arrange
        var client = await CreateAuthenticatedAuthClientAsync();

        // Act
        var response = await client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact(DisplayName = "Logging out without a token should return 401.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_Logout_WithoutToken_ShouldReturn401()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "Registering with invalid email should return 400.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_Register_InvalidEmail_ShouldReturn400()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("not-an-email", "Test1234!"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(DisplayName = "Health endpoint should return healthy.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_Health_ShouldReturnHealthy()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(IntegrationTestsFixture.JsonOptions);
        Assert.Equal("healthy", body?["status"]);
    }

    [Fact(DisplayName = "JWT from Auth API should be accepted by Tasks API.")]
    [Trait("Category", "Integration Web - Cross Host")]
    public async Task CrossHost_AuthToken_ShouldAccessTasksApi()
    {
        // Arrange
        var client = await _fixture.CreateAuthenticatedTasksClientAsync();

        // Act
        var response = await client.GetAsync("/api/tasks");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedAuthClientAsync()
    {
        var client = _fixture.AuthFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(IntegrationTestsFixture.DemoEmail, IntegrationTestsFixture.DemoPassword));
        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(IntegrationTestsFixture.JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }
}
