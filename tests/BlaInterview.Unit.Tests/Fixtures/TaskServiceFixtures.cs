using AutoMapper;
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

public class TaskServiceFixtures
{
    public AutoMocker Mocker { get; private set; } = null!;

    public TaskService GetService()
    {
        Mocker = new AutoMocker();
        Mocker.Use<IMapper>(MapperFixtures.CreateMapper());
        Mocker.Use<IValidator<CreateTaskBody>>(new CreateTaskBodyValidator());
        Mocker.Use<IValidator<UpdateTaskBody>>(new UpdateTaskBodyValidator());
        Mocker.Use<IValidator<TaskFilterRequest>>(new TaskFilterRequestValidator());
        return Mocker.CreateInstance<TaskService>();
    }

    public CreateTaskBody GenerateValidCreateRequest()
    {
        return new Faker<CreateTaskBody>()
            .CustomInstantiator(f => new CreateTaskBody(
                f.Lorem.Sentence(3),
                f.Lorem.Paragraph(),
                f.PickRandom<TaskPriority>(),
                f.Date.FutureOffset(7)))
            .Generate();
    }

    public CreateTaskBody GenerateInvalidCreateRequestEmptyTitle()
    {
        return new CreateTaskBody(string.Empty, null, TaskPriority.Medium, null);
    }

    public CreateTaskBody GenerateInvalidCreateRequestPastDueDate()
    {
        return new CreateTaskBody(
            "Past due task",
            "A description",
            TaskPriority.Medium,
            DateTimeOffset.UtcNow.AddDays(-1));
    }

    public UpdateTaskBody GenerateInvalidUpdateRequestPastDueDate(KanbanStatus status = KanbanStatus.Todo)
    {
        return new UpdateTaskBody(
            "Past due task",
            "A description",
            status,
            TaskPriority.Medium,
            DateTimeOffset.UtcNow.AddDays(-1));
    }

    public UpdateTaskBody GenerateValidUpdateRequest(
        KanbanStatus status = KanbanStatus.InProgress,
        TaskPriority priority = TaskPriority.Medium)
    {
        return new Faker<UpdateTaskBody>()
            .CustomInstantiator(f => new UpdateTaskBody(
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
}
