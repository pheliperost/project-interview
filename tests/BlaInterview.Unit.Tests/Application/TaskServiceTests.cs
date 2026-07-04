using BlaInterview.Application.Common;
using BlaInterview.Application.DTOs;
using BlaInterview.Application.Interfaces;
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
        Assert.False(string.IsNullOrWhiteSpace(result.Title));
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.AddAsync(It.IsAny<TaskItem>(), default), Times.Once);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact(DisplayName = "Creating a task with empty title should return 400.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_CreateTask_EmptyTitle_ShouldThrow400()
    {
        // Arrange
        var request = _fixtures.GenerateInvalidCreateRequestEmptyTitle();

        // Act
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateTaskAsync("user1", request));

        // Assert
        Assert.Equal(400, ex.StatusCode);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.AddAsync(It.IsAny<TaskItem>(), default), Times.Never);
    }

    [Fact(DisplayName = "Creating a task with past due date should return 400.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_CreateTask_PastDueDate_ShouldThrow400()
    {
        // Arrange
        var request = _fixtures.GenerateInvalidCreateRequestPastDueDate();

        // Act
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.CreateTaskAsync("user1", request));

        // Assert
        Assert.Equal(400, ex.StatusCode);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.AddAsync(It.IsAny<TaskItem>(), default), Times.Never);
    }

    [Theory(DisplayName = "Updating status from a terminal task should return 400.")]
    [Trait("Category", "Task Service")]
    [MemberData(nameof(KanbanStatusTransitionData.TerminalStatusChangeAttempts), MemberType = typeof(KanbanStatusTransitionData))]
    public async Task TaskService_UpdateStatus_FromTerminal_ShouldThrow400(KanbanStatus currentStatus, KanbanStatus targetStatus)
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = _fixtures.GenerateOwnedTask("user1", currentStatus);

        _fixtures.Mocker.GetMock<ITaskRepository>()
            .Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        var request = _fixtures.GenerateValidUpdateRequest(targetStatus);

        // Act
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.UpdateTaskAsync("user1", taskId, request));

        // Assert
        Assert.Equal(400, ex.StatusCode);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), default), Times.Never);
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
        Assert.Equal(targetStatus, result.Status);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), default), Times.Once);
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
        Assert.Equal(KanbanStatus.Todo, result.Status);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), default), Times.Once);
    }

    [Fact(DisplayName = "Reactivating an active task should return 400.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_Reactivate_ActiveTask_ShouldThrow400()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = _fixtures.GenerateOwnedTask("user1", KanbanStatus.InProgress);

        _fixtures.Mocker.GetMock<ITaskRepository>()
            .Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.ReactivateAsync("user1", taskId));

        // Assert
        Assert.Equal(400, ex.StatusCode);
        _fixtures.Mocker.GetMock<ITaskRepository>().Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), default), Times.Never);
    }

    [Fact(DisplayName = "Getting another user's task should return 403.")]
    [Trait("Category", "Task Service")]
    public async Task TaskService_GetTask_OtherUsersTask_ShouldThrow403()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = _fixtures.GenerateOwnedTask("other", KanbanStatus.Todo);

        _fixtures.Mocker.GetMock<ITaskRepository>()
            .Setup(r => r.GetByIdAsync(taskId, default))
            .ReturnsAsync(task);

        // Act
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            _service.GetTaskByIdAsync("user1", taskId));

        // Assert
        Assert.Equal(403, ex.StatusCode);
    }
}
