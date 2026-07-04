using BlaInterview.Application.DTOs;
using BlaInterview.Application.Interfaces;
using BlaInterview.Application.Services;
using BlaInterview.Application.Validators;
using BlaInterview.Domain.Entities;
using BlaInterview.Domain.Enums;
using Bogus;
using FluentValidation;
using Moq;
using Moq.AutoMock;

namespace BlaInterview.Unit.Tests.Fixtures;

[CollectionDefinition(nameof(TaskServiceCollection))]
public class TaskServiceCollection : ICollectionFixture<TaskServiceFixtures>
{
}

public class TaskServiceFixtures : IDisposable
{
    public AutoMocker Mocker { get; private set; } = null!;

    public TaskService GetService()
    {
        Mocker = new AutoMocker();
        Mocker.Use<IValidator<CreateTaskRequest>>(new CreateTaskRequestValidator());
        Mocker.Use<IValidator<UpdateTaskRequest>>(new UpdateTaskRequestValidator());
        Mocker.Use<IValidator<TaskFilterRequest>>(new TaskFilterRequestValidator());
        return Mocker.CreateInstance<TaskService>();
    }

    public CreateTaskRequest GenerateValidCreateRequest()
    {
        return new Faker<CreateTaskRequest>()
            .CustomInstantiator(f => new CreateTaskRequest(
                f.Lorem.Sentence(3),
                f.Lorem.Paragraph(),
                f.PickRandom<TaskPriority>(),
                f.Date.FutureOffset(7)))
            .Generate();
    }

    public CreateTaskRequest GenerateInvalidCreateRequestEmptyTitle()
    {
        return new CreateTaskRequest(string.Empty, null, TaskPriority.Medium, null);
    }

    public CreateTaskRequest GenerateInvalidCreateRequestPastDueDate()
    {
        return new CreateTaskRequest(
            "Past due task",
            null,
            TaskPriority.Medium,
            DateTimeOffset.UtcNow.AddDays(-1));
    }

    public UpdateTaskRequest GenerateValidUpdateRequest(
        KanbanStatus status = KanbanStatus.InProgress,
        TaskPriority priority = TaskPriority.Medium)
    {
        return new Faker<UpdateTaskRequest>()
            .CustomInstantiator(f => new UpdateTaskRequest(
                f.Lorem.Sentence(3),
                f.Lorem.Paragraph(),
                status,
                priority,
                f.Date.FutureOffset(7)))
            .Generate();
    }

    public TaskItem GenerateOwnedTask(string userId, KanbanStatus status)
    {
        var task = DomainFixtures.GenerateValidTaskItem(userId, status);
        task.Status = status;
        return task;
    }

    public void Dispose()
    {
    }
}
