using BlaInterview.Domain.Enums;
using BlaInterview.Unit.Tests.Data;
using BlaInterview.Unit.Tests.Fixtures;

namespace BlaInterview.Unit.Tests.Domain;

public class TaskItemTests
{
    [Theory(DisplayName = "Terminal Kanban status should report IsTerminal as true.")]
    [Trait("Category", "Domain Entity")]
    [MemberData(nameof(TaskItemTerminalData.TerminalStatuses), MemberType = typeof(TaskItemTerminalData))]
    public void TaskItem_IsTerminal_WhenTerminalStatus_ShouldReturnTrue(KanbanStatus status, bool expected)
    {
        // Arrange
        var task = DomainFixtures.GenerateValidTaskItem(status: status);
        task.Status = status;

        // Act
        var result = task.IsTerminal;

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "Active Kanban status should report IsTerminal as false.")]
    [Trait("Category", "Domain Entity")]
    [MemberData(nameof(TaskItemTerminalData.ActiveStatuses), MemberType = typeof(TaskItemTerminalData))]
    public void TaskItem_IsTerminal_WhenActiveStatus_ShouldReturnFalse(KanbanStatus status, bool expected)
    {
        // Arrange
        var task = DomainFixtures.GenerateValidTaskItem(status: status);
        task.Status = status;

        // Act
        var result = task.IsTerminal;

        // Assert
        Assert.Equal(expected, result);
    }
}
