using BlaInterview.Domain.Enums;

namespace BlaInterview.Unit.Tests.Data;

public static class KanbanStatusTransitionData
{
    public static IEnumerable<object[]> TerminalStatusChangeAttempts()
    {
        yield return new object[] { KanbanStatus.Completed, KanbanStatus.InProgress };
        yield return new object[] { KanbanStatus.Completed, KanbanStatus.Todo };
        yield return new object[] { KanbanStatus.Cancelled, KanbanStatus.InReview };
        yield return new object[] { KanbanStatus.Cancelled, KanbanStatus.Todo };
    }

    public static IEnumerable<object[]> ValidActiveTransitions()
    {
        yield return new object[] { KanbanStatus.Todo, KanbanStatus.OnHold };
        yield return new object[] { KanbanStatus.InProgress, KanbanStatus.InReview };
        yield return new object[] { KanbanStatus.OnHold, KanbanStatus.InProgress };
    }
}
