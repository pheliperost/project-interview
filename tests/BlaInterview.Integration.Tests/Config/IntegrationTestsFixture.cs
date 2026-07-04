using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlaInterview.Application.DTOs;
using BlaInterview.Domain.Enums;
using Bogus;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BlaInterview.Integration.Tests.Config;

[CollectionDefinition(nameof(IntegrationWebTestsFixtureCollection), DisableParallelization = true)]
public class IntegrationWebTestsFixtureCollection : ICollectionFixture<IntegrationTestsFixture>
{
}

public class IntegrationTestsFixture : IDisposable
{
    public const string DemoEmail = "demo@bla.local";
    public const string DemoPassword = "Demo123!";

    public BlaInterviewAppFactory Factory { get; }
    public HttpClient Client { get; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public IntegrationTestsFixture()
    {
        Factory = new BlaInterviewAppFactory();
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public CreateTaskRequest GenerateValidCreateRequest()
    {
        return new Faker<CreateTaskRequest>()
            .CustomInstantiator(f => new CreateTaskRequest(
                f.Lorem.Sentence(3),
                f.Lorem.Paragraph(),
                TaskPriority.Medium,
                f.Date.FutureOffset(7)))
            .Generate();
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(DemoEmail, DemoPassword));
        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        if (string.IsNullOrWhiteSpace(auth?.Token))
            throw new InvalidOperationException("Login did not return a JWT.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
    }
}
