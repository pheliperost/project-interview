using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlaInterview.Application.DTOs;
using BlaInterview.Domain.Enums;
using BlaInterview.Infrastructure.Seeding;
using Bogus;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BlaInterview.Integration.Tests.Config;

[CollectionDefinition(nameof(IntegrationWebTestsFixtureCollection), DisableParallelization = true)]
public class IntegrationWebTestsFixtureCollection : ICollectionFixture<IntegrationTestsFixture>
{
}

public class IntegrationTestsFixture : IDisposable
{
    public const string DemoEmail = UserDatabaseSeeder.DemoEmail;
    public const string DemoPassword = UserDatabaseSeeder.DemoPassword;
    public const string OtherEmail = UserDatabaseSeeder.OtherEmail;
    public const string OtherPassword = UserDatabaseSeeder.OtherPassword;

    public AuthAppFactory AuthFactory { get; }
    public TasksAppFactory TasksFactory { get; }

    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public IntegrationTestsFixture()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"simple-tasks-tests-{Guid.NewGuid():N}.db");
        AuthFactory = new AuthAppFactory(databasePath);
        TasksFactory = new TasksAppFactory(databasePath);

        AuthFactory.CreateClient();
        TasksFactory.CreateClient();
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

    public async Task<HttpClient> CreateAuthenticatedTasksClientAsync()
        => await CreateAuthenticatedTasksClientAsync(DemoEmail, DemoPassword);

    public async Task<HttpClient> CreateAuthenticatedTasksClientAsync(string email, string password)
    {
        var authClient = AuthFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginResponse = await authClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        if (string.IsNullOrWhiteSpace(auth?.Token))
            throw new InvalidOperationException("Login did not return a JWT.");

        var tasksClient = TasksFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        tasksClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return tasksClient;
    }

    public async Task<Guid> GetSeededOtherUserTaskIdAsync()
    {
        var otherClient = await CreateAuthenticatedTasksClientAsync(OtherEmail, OtherPassword);
        var response = await otherClient.GetAsync("/api/tasks");
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<TaskListResponse>(JsonOptions);
        var task = list?.Items.FirstOrDefault(t => t.Title.StartsWith("[Other]", StringComparison.Ordinal));
        if (task is null)
            throw new InvalidOperationException("Seeded other-user task not found.");

        return task.Id;
    }

    public async Task<TaskResponse> GetDemoTaskAsync(KanbanStatus? status = null, string? titleContains = null)
    {
        var client = await CreateAuthenticatedTasksClientAsync();
        var path = status.HasValue ? $"/api/tasks?status={status.Value}" : "/api/tasks";
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<TaskListResponse>(JsonOptions);
        var task = list?.Items.FirstOrDefault(t =>
            (titleContains is null || t.Title.Contains(titleContains, StringComparison.OrdinalIgnoreCase))
            && (!status.HasValue || t.Status == status.Value));

        if (task is null)
            throw new InvalidOperationException("Demo task matching criteria not found.");

        return task;
    }

    public async Task<TaskResponse> CreateDemoTaskAsync(CreateTaskRequest? request = null)
    {
        var client = await CreateAuthenticatedTasksClientAsync();
        request ??= GenerateValidCreateRequest();
        var response = await client.PostAsJsonAsync("/api/tasks", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions))!;
    }

    public void Dispose()
    {
        AuthFactory.Dispose();
        TasksFactory.Dispose();
    }
}
