using BlaInterview.Domain.Enums;

namespace BlaInterview.Unit.Tests.Data;

public static class TaskItemTerminalData
{
    public static IEnumerable<object[]> TerminalStatuses =>
        new List<object[]>
        {
            new object[] { KanbanStatus.Completed, true },
            new object[] { KanbanStatus.Cancelled, true }
        };

    public static IEnumerable<object[]> ActiveStatuses =>
        new List<object[]>
        {
            new object[] { KanbanStatus.Todo, false },
            new object[] { KanbanStatus.InProgress, false },
            new object[] { KanbanStatus.OnHold, false },
            new object[] { KanbanStatus.InReview, false }
        };
}
