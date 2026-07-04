using BlaInterview.Domain.Entities;
using BlaInterview.Domain.Enums;
using BlaInterview.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlaInterview.Infrastructure.Seeding;

public static class TaskDatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        await context.Database.MigrateAsync();

        var demo = await context.Users.FirstOrDefaultAsync(u => u.Email == UserDatabaseSeeder.DemoEmail);
        var other = await context.Users.FirstOrDefaultAsync(u => u.Email == UserDatabaseSeeder.OtherEmail);

        if (demo is null || other is null)
        {
            logger.LogWarning("Skipping task seeding — demo users not found. Start Auth API first.");
            return;
        }

        var seeded = 0;

        if (!await context.Tasks.AnyAsync(t => t.UserId == demo.Id))
            seeded += await SeedDemoUserTasksAsync(context, demo.Id);

        if (!await context.Tasks.AnyAsync(t => t.UserId == other.Id))
            seeded += await SeedOtherUserTasksAsync(context, other.Id);

        if (seeded > 0)
            logger.LogInformation("Database seeded with {Count} tasks.", seeded);
    }

    private static async Task<int> SeedDemoUserTasksAsync(AppDbContext context, string userId)
    {
        var now = DateTimeOffset.UtcNow;
        var tasks = new List<TaskItem>
        {
            CreateTask(userId, "Plan sprint backlog", "Review priorities for the week", KanbanStatus.Todo, TaskPriority.High, now.AddDays(3), now),
            CreateTask(userId, "Refine API integration", "Update hooks for streaming parameters", KanbanStatus.Todo, TaskPriority.Urgent, now.AddDays(1), now),
            CreateTask(userId, "Model training pipeline", "Optimize batch processing", KanbanStatus.InProgress, TaskPriority.Medium, now.AddDays(5), now),
            CreateTask(userId, "Data node mapping", "Align schema with new nodes", KanbanStatus.InProgress, TaskPriority.Low, now.AddDays(7), now),
            CreateTask(userId, "Waiting on design assets", "Blocked by brand team", KanbanStatus.OnHold, TaskPriority.Medium, now.AddDays(10), now),
            CreateTask(userId, "Visual flow schema", "Awaiting stakeholder review", KanbanStatus.InReview, TaskPriority.High, now.AddDays(2), now),
            CreateTask(userId, "Ship release notes", "Finalize changelog", KanbanStatus.Completed, TaskPriority.Medium, now.AddDays(-1), now),
            CreateTask(userId, "Deprecated feature cleanup", "Remove legacy endpoints", KanbanStatus.Cancelled, TaskPriority.Low, now.AddDays(4), now),
        };

        context.Tasks.AddRange(tasks);
        await context.SaveChangesAsync();
        return tasks.Count;
    }

    private static async Task<int> SeedOtherUserTasksAsync(AppDbContext context, string userId)
    {
        var now = DateTimeOffset.UtcNow;
        var tasks = new List<TaskItem>
        {
            CreateTask(userId, "[Other] Team standup notes", "Weekly sync items", KanbanStatus.InProgress, TaskPriority.Medium, now.AddDays(2), now),
            CreateTask(userId, "[Other] Budget review", "Q3 numbers", KanbanStatus.InProgress, TaskPriority.High, now.AddDays(6), now),
            CreateTask(userId, "[Other] Archive old tickets", "Close stale items", KanbanStatus.Completed, TaskPriority.Low, now.AddDays(-2), now),
            CreateTask(userId, "[Other] Cancelled initiative", "No longer pursuing", KanbanStatus.Cancelled, TaskPriority.Medium, now.AddDays(8), now),
        };

        context.Tasks.AddRange(tasks);
        await context.SaveChangesAsync();
        return tasks.Count;
    }

    private static TaskItem CreateTask(string userId, string title, string? description, KanbanStatus status, TaskPriority priority, DateTimeOffset dueDate, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Title = title,
        Description = description,
        Status = status,
        Priority = priority,
        DueDate = dueDate,
        CreatedAt = now.AddDays(-Random.Shared.Next(1, 14)),
        UpdatedAt = now
    };
}
