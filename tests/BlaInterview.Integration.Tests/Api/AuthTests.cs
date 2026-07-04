using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlaInterview.Application.DTOs;
using BlaInterview.Integration.Tests.Config;

namespace BlaInterview.Integration.Tests.Api;

[Collection(nameof(IntegrationWebTestsFixtureCollection))]
public class AuthTests
{
    private readonly IntegrationTestsFixture _fixture;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthTests(IntegrationTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(DisplayName = "Getting tasks without a token should return 401.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_GetTasks_WithoutToken_ShouldReturn401()
    {
        // Arrange
        var client = _fixture.TasksFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/tasks");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "Logging in with demo credentials should return a JWT.")]
    [Trait("Category", "Integration Web - Auth")]
    public async Task Auth_Login_WithDemoUser_ShouldReturnToken()
    {
        // Arrange
        var client = _fixture.AuthFactory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(IntegrationTestsFixture.DemoEmail, IntegrationTestsFixture.DemoPassword));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.False(string.IsNullOrWhiteSpace(auth?.Token));
    }
}
