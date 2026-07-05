using BlaInterview.Application.DTOs;
using BlaInterview.Application.Mapping;
using FluentValidation;

namespace BlaInterview.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.DueDate)
            .Must(d => d == null || d >= DateTimeOffset.UtcNow.Date)
            .WithMessage("Due date cannot be in the past.");
    }
}

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.DueDate)
            .Must(d => d == null || d >= DateTimeOffset.UtcNow.Date)
            .WithMessage("Due date cannot be in the past.");
    }
}

public class TaskFilterRequestValidator : AbstractValidator<TaskFilterRequest>
{
    public TaskFilterRequestValidator()
    {
        RuleFor(x => x)
            .Must(f => f.CreatedFrom == null || f.CreatedTo == null || f.CreatedFrom <= f.CreatedTo)
            .WithMessage("Created date range is invalid.");
        RuleFor(x => x)
            .Must(f => f.UpdatedFrom == null || f.UpdatedTo == null || f.UpdatedFrom <= f.UpdatedTo)
            .WithMessage("Updated date range is invalid.");
        RuleFor(x => x.Page).GreaterThan(0).When(x => x.Page.HasValue);
        RuleFor(x => x.PageSize).InclusiveBetween(1, TaskPagination.MaxPageSize).When(x => x.PageSize.HasValue);
    }
}
