using BlaInterview.Domain.Entities;
using BlaInterview.Domain.Enums;
using Bogus;

namespace BlaInterview.Unit.Tests.Fixtures;

public static class DomainFixtures
{
    public static TaskItem GenerateValidTaskItem(string userId = "user1", KanbanStatus? status = null)
    {
        var faker = new Faker();
        var now = DateTimeOffset.UtcNow;

        return new Faker<TaskItem>()
            .RuleFor(t => t.Id, _ => Guid.NewGuid())
            .RuleFor(t => t.UserId, _ => userId)
            .RuleFor(t => t.Title, f => f.Lorem.Sentence(3))
            .RuleFor(t => t.Description, f => f.Lorem.Paragraph())
            .RuleFor(t => t.Status, (f, _) => status ?? f.PickRandom(
                KanbanStatus.Todo,
                KanbanStatus.InProgress,
                KanbanStatus.OnHold,
                KanbanStatus.InReview))
            .RuleFor(t => t.Priority, f => f.PickRandom<TaskPriority>())
            .RuleFor(t => t.DueDate, f => f.Date.FutureOffset(14))
            .RuleFor(t => t.CreatedAt, _ => now)
            .RuleFor(t => t.UpdatedAt, _ => now)
            .Generate();
    }

    public static TaskItem GenerateTerminalTaskItem(string userId = "user1", KanbanStatus? status = null)
    {
        var terminalStatus = status ?? KanbanStatus.Completed;
        if (terminalStatus is not (KanbanStatus.Completed or KanbanStatus.Cancelled))
        {
            terminalStatus = KanbanStatus.Completed;
        }

        var task = GenerateValidTaskItem(userId, terminalStatus);
        task.Status = terminalStatus;
        return task;
    }

    public static TaskItem GenerateInvalidTaskItemMissingTitle(string userId = "user1")
    {
        return new TaskItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = string.Empty,
            Status = KanbanStatus.Todo,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
