using System.Data.Common;
using BlaInterview.Domain.Entities;
using BlaInterview.Domain.Enums;
using BlaInterview.Infrastructure.Persistence;
using BlaInterview.Infrastructure.Repositories;
using Bogus;
using Microsoft.EntityFrameworkCore;

namespace BlaInterview.Integration.Tests.Fixtures;

[CollectionDefinition(nameof(TaskRepositoryCollection))]
public class TaskRepositoryCollection : ICollectionFixture<TaskRepositoryFixtures>
{
}

public class TaskRepositoryFixtures : IDisposable
{
    private readonly DbConnection _connection;
    public AppDbContext Context { get; }
    public TaskRepository Repository { get; }

    public TaskRepositoryFixtures()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        Context = new AppDbContext(options);
        _connection = Context.Database.GetDbConnection();
        _connection.Open();
        Context.Database.EnsureCreated();
        Repository = new TaskRepository(Context);
    }

    public async Task SeedDefaultTasksAsync()
    {
        Context.Tasks.RemoveRange(Context.Tasks);
        await Context.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        Context.Tasks.AddRange(
            GenerateTask("user1", "Refine API hooks", KanbanStatus.Todo, now),
            GenerateTask("user1", "Ship release", KanbanStatus.Completed, now),
            GenerateTask("user2", "Other API task", KanbanStatus.Todo, now));
        await Context.SaveChangesAsync();
    }

    public TaskItem GenerateTask(string userId, string title, KanbanStatus status, DateTimeOffset timestamp)
    {
        return new Faker<TaskItem>()
            .RuleFor(t => t.Id, _ => Guid.NewGuid())
            .RuleFor(t => t.UserId, _ => userId)
            .RuleFor(t => t.Title, _ => title)
            .RuleFor(t => t.Description, f => f.Lorem.Sentence())
            .RuleFor(t => t.Status, _ => status)
            .RuleFor(t => t.Priority, f => f.PickRandom<TaskPriority>())
            .RuleFor(t => t.CreatedAt, _ => timestamp)
            .RuleFor(t => t.UpdatedAt, _ => timestamp)
            .Generate();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
