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

    public TaskItem GenerateTask(
        string userId,
        string title,
        KanbanStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? dueDate = null)
    {
        var task = GenerateTask(userId, title, status, createdAt);
        task.CreatedAt = createdAt;
        task.UpdatedAt = updatedAt;
        task.DueDate = dueDate;
        return task;
    }

    public async Task SeedDatedTasksAsync()
    {
        Context.Tasks.RemoveRange(Context.Tasks);
        await Context.SaveChangesAsync();

        var baseDate = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        Context.Tasks.AddRange(
            GenerateTask("user1", "Early task", KanbanStatus.Todo, baseDate.AddDays(-5), baseDate.AddDays(-5), baseDate.AddDays(1)),
            GenerateTask("user1", "Late task", KanbanStatus.Todo, baseDate.AddDays(5), baseDate.AddDays(5), baseDate.AddDays(10)),
            GenerateTask("user1", "No due date", KanbanStatus.Todo, baseDate, baseDate, null),
            GenerateTask("user2", "Other user task", KanbanStatus.Todo, baseDate, baseDate, baseDate.AddDays(2)));
        await Context.SaveChangesAsync();
    }

    public async Task SeedDateRangeTasksAsync()
    {
        Context.Tasks.RemoveRange(Context.Tasks);
        await Context.SaveChangesAsync();

        var jan1 = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var jan10 = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var jan20 = new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero);
        var feb1 = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        Context.Tasks.AddRange(
            GenerateTask("user1", "January task", KanbanStatus.Todo, jan1, jan10),
            GenerateTask("user1", "February task", KanbanStatus.Todo, feb1, feb1),
            GenerateTask("user1", "Updated in January", KanbanStatus.Todo, jan1, jan20));
        await Context.SaveChangesAsync();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
