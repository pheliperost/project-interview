using BlaInterview.Application.DTOs;
using BlaInterview.Application.Interfaces;
using BlaInterview.Application.Notifications;
using BlaInterview.Application.Queries;
using BlaInterview.Application.Services;
using BlaInterview.Domain.Entities;
using BlaInterview.Domain.Enums;
using BlaInterview.Unit.Tests.Data;
using BlaInterview.Unit.Tests.Fixtures;
using Moq;

namespace BlaInterview.Unit.Tests.Application;

[Collection(nameof(TaskServiceCollection))]
public class TaskServiceTests
{
    private readonly TaskServiceFixtures _fixtures;
    private readonly TaskService _service;

    public TaskServiceTests(TaskServiceFixtures fixtures)
    {
        _fixtures = fixtures;
        _service = fixtures.GetService();
    }

    [Fact(DisplayName = "Creating a valid task should persist through the repository.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_CreateTask_ValidRequest_ShouldPersist()
    {
        // Arrange
        var request = _fixtures.GenerateValidCreateRequest();

        // Act
        var result = await _service.CreateTaskAsync("user1", request);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Title));
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.AddAsync(It.IsAny<TaskItem>(), default), Times.Once);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.SaveChangesAsync(default), Times.Once);
        _fixtures.Mocker.GetMock<INotifyer>().Verify(m => m.Handle(It.IsAny<Notification>()), Times.Never);
    }

    [Fact(DisplayName = "Creating a task with empty title should notify validation error.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_CreateTask_EmptyTitle_ShouldNotify()
    {
        // Arrange
        var request = _fixtures.GenerateInvalidCreateRequestEmptyTitle();

        // Act
        var result = await _service.CreateTaskAsync("user1", request);

        // Assert
        Assert.Null(result);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.AddAsync(It.IsAny<TaskItem>(), default), Times.Never);
        _fixtures.Mocker.GetMock<INotifyer>().Verify(m => m.Handle(It.IsAny<Notification>()), Times.AtLeastOnce);
    }

    [Fact(DisplayName = "Creating a task with past due date should notify validation error.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_CreateTask_PastDueDate_ShouldNotify()
    {
        // Arrange
        var request = _fixtures.GenerateInvalidCreateRequestPastDueDate();

        // Act
        var result = await _service.CreateTaskAsync("user1", request);

        // Assert
        Assert.Null(result);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.AddAsync(It.IsAny<TaskItem>(), default), Times.Never);
        _fixtures.Mocker.GetMock<INotifyer>().Verify(m => m.Handle(It.IsAny<Notification>()), Times.AtLeastOnce);
    }

    [Theory(DisplayName = "Updating status from a terminal task should notify.")]
    [Trait("Category", "Task Service")]
    [MemberData(nameof(KanbanStatusTransitionData.TerminalStatusChangeAttempts), MemberType = typeof(KanbanStatusTransitionData))]
    public async Task TaskService_UpdateStatus_FromTerminal_ShouldNotify(KanbanStatus currentStatus, KanbanStatus targetStatus)
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = _fixtures.GenerateOwnedTask("user1", currentStatus);

        _fixtures.Mocker.GetMock<ITaskRepository>()
            .Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        var request = _fixtures.GenerateValidUpdateRequest(targetStatus);

        // Act
        var result = await _service.UpdateTaskAsync("user1", taskId, request);

        // Assert
        Assert.Null(result);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), default), Times.Never);
        _fixtures.Mocker.GetMock<INotifyer>().Verify(m => m.Handle(It.IsAny<Notification>()), Times.AtLeastOnce);
    }

    [Theory(DisplayName = "Updating between active statuses should succeed.")]
    [Trait("Category", "Task Service")]
    [MemberData(nameof(KanbanStatusTransitionData.ValidActiveTransitions), MemberType = typeof(KanbanStatusTransitionData))]
    public async Task TaskService_UpdateStatus_ActiveTransition_ShouldSucceed(KanbanStatus currentStatus, KanbanStatus targetStatus)
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = _fixtures.GenerateOwnedTask("user1", currentStatus);

        _fixtures.Mocker.GetMock<ITaskRepository>()
            .Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        var request = _fixtures.GenerateValidUpdateRequest(targetStatus);

        // Act
        var result = await _service.UpdateTaskAsync("user1", taskId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(targetStatus, result!.Status);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), default), Times.Once);
        _fixtures.Mocker.GetMock<INotifyer>().Verify(m => m.Handle(It.IsAny<Notification>()), Times.Never);
    }

    [Fact(DisplayName = "Reactivating a completed task should return To Do.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_Reactivate_CompletedTask_ShouldReturnTodo()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = _fixtures.GenerateOwnedTask("user1", KanbanStatus.Completed);

        _fixtures.Mocker.GetMock<ITaskRepository>()
            .Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var result = await _service.ReactivateAsync("user1", taskId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(KanbanStatus.Todo, result!.Status);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), default), Times.Once);
        _fixtures.Mocker.GetMock<INotifyer>().Verify(m => m.Handle(It.IsAny<Notification>()), Times.Never);
    }

    [Fact(DisplayName = "Reactivating an active task should notify.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_Reactivate_ActiveTask_ShouldNotify()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = _fixtures.GenerateOwnedTask("user1", KanbanStatus.InProgress);

        _fixtures.Mocker.GetMock<ITaskRepository>()
            .Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var result = await _service.ReactivateAsync("user1", taskId);

        // Assert
        Assert.Null(result);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), default), Times.Never);
        _fixtures.Mocker.GetMock<INotifyer>().Verify(m => m.Handle(It.IsAny<Notification>()), Times.AtLeastOnce);
    }

    [Fact(DisplayName = "Getting another user's task should notify forbidden.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_GetTask_OtherUsersTask_ShouldNotify403()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = _fixtures.GenerateOwnedTask("other", KanbanStatus.Todo);

        _fixtures.Mocker.GetMock<ITaskRepository>()
            .Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var result = await _service.GetTaskByIdAsync("user1", taskId);

        // Assert
        Assert.Null(result);
        _fixtures.Mocker.GetMock<INotifyer>().Verify(
            m => m.Handle(It.Is<Notification>(n => n.StatusCode == 403)),
            Times.Once);
    }

    [Fact(DisplayName = "Updating another user's task should notify forbidden.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_UpdateTask_OtherUsersTask_ShouldNotify403()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = _fixtures.GenerateOwnedTask("other", KanbanStatus.InProgress);
        var request = _fixtures.GenerateValidUpdateRequest();

        _fixtures.Mocker.GetMock<ITaskRepository>()
            .Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var result = await _service.UpdateTaskAsync("user1", taskId, request);

        // Assert
        Assert.Null(result);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), default), Times.Never);
        _fixtures.Mocker.GetMock<INotifyer>().Verify(
            m => m.Handle(It.Is<Notification>(n => n.StatusCode == 403)),
            Times.Once);
    }

    [Fact(DisplayName = "Deleting another user's task should notify forbidden.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_DeleteTask_OtherUsersTask_ShouldNotify403()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = _fixtures.GenerateOwnedTask("other", KanbanStatus.Todo);

        _fixtures.Mocker.GetMock<ITaskRepository>()
            .Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        await _service.DeleteTaskAsync("user1", taskId);

        // Assert
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.DeleteAsync(It.IsAny<TaskItem>(), default), Times.Never);
        _fixtures.Mocker.GetMock<INotifyer>().Verify(
            m => m.Handle(It.Is<Notification>(n => n.StatusCode == 403)),
            Times.Once);
    }

    [Fact(DisplayName = "Reactivating another user's task should notify forbidden.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_ReactivateTask_OtherUsersTask_ShouldNotify403()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = _fixtures.GenerateOwnedTask("other", KanbanStatus.Completed);

        _fixtures.Mocker.GetMock<ITaskRepository>()
            .Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var result = await _service.ReactivateAsync("user1", taskId);

        // Assert
        Assert.Null(result);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), default), Times.Never);
        _fixtures.Mocker.GetMock<INotifyer>().Verify(
            m => m.Handle(It.Is<Notification>(n => n.StatusCode == 403)),
            Times.Once);
    }

    [Fact(DisplayName = "Deleting an owned task should remove it from the repository.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_DeleteTask_OwnedTask_ShouldPersist()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = _fixtures.GenerateOwnedTask("user1", KanbanStatus.Todo);

        _fixtures.Mocker.GetMock<ITaskRepository>()
            .Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        await _service.DeleteTaskAsync("user1", taskId);

        // Assert
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.DeleteAsync(task, default), Times.Once);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact(DisplayName = "Getting a missing task should notify not found.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_GetTask_NotFound_ShouldNotify404()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _fixtures.Mocker.GetMock<ITaskRepository>()
            .Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _service.GetTaskByIdAsync("user1", taskId);

        // Assert
        Assert.Null(result);
        _fixtures.Mocker.GetMock<INotifyer>().Verify(
            m => m.Handle(It.Is<Notification>(n => n.StatusCode == 404)),
            Times.Once);
    }

    [Fact(DisplayName = "Listing tasks with invalid date range should notify.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_GetTasks_InvalidDateRange_ShouldNotify()
    {
        // Arrange
        var filter = new TaskFilterRequest(null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-1), null, null, null, null);

        // Act
        var result = await _service.GetTasksAsync("user1", filter);

        // Assert
        Assert.Null(result);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.GetByUserAsync(It.IsAny<string>(), It.IsAny<TaskQuery>(), default), Times.Never);
        _fixtures.Mocker.GetMock<INotifyer>().Verify(m => m.Handle(It.IsAny<Notification>()), Times.AtLeastOnce);
    }

    [Fact(DisplayName = "Updating a terminal task without changing status should succeed.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_UpdateTask_TerminalSameStatus_ShouldSucceed()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = _fixtures.GenerateOwnedTask("user1", KanbanStatus.Completed);
        var request = _fixtures.GenerateValidUpdateRequest(KanbanStatus.Completed);

        _fixtures.Mocker.GetMock<ITaskRepository>()
            .Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var result = await _service.UpdateTaskAsync("user1", taskId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(KanbanStatus.Completed, result!.Status);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), default), Times.Once);
    }
}
