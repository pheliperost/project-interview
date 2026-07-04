using BlaInterview.Domain.Enums;

namespace BlaInterview.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public KanbanStatus Status { get; set; } = KanbanStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTimeOffset? DueDate { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsTerminal => Status is KanbanStatus.Completed or KanbanStatus.Cancelled;
}
