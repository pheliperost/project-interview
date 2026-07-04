using BlaInterview.Application.DTOs;
using BlaInterview.Application.Mapping;
using BlaInterview.Application.Validators;
using BlaInterview.Domain.Enums;

namespace BlaInterview.Unit.Tests.Application;

public class ValidatorTests
{
    private readonly CreateTaskRequestValidator _createValidator = new();
    private readonly UpdateTaskRequestValidator _updateValidator = new();
    private readonly TaskFilterRequestValidator _filterValidator = new();
    private readonly RegisterRequestValidator _registerValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();

    [Fact(DisplayName = "Create task validator should reject empty title.")]
    [Trait("Category", "Validators")]
    public void CreateTaskValidator_EmptyTitle_ShouldBeInvalid()
    {
        var result = _createValidator.Validate(new CreateTaskRequest("", null, TaskPriority.Medium, null));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Create task validator should reject past due date.")]
    [Trait("Category", "Validators")]
    public void CreateTaskValidator_PastDueDate_ShouldBeInvalid()
    {
        var result = _createValidator.Validate(new CreateTaskRequest("Title", null, TaskPriority.Medium, DateTimeOffset.UtcNow.AddDays(-1)));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Create task validator should accept null due date.")]
    [Trait("Category", "Validators")]
    public void CreateTaskValidator_NullDueDate_ShouldBeValid()
    {
        var result = _createValidator.Validate(new CreateTaskRequest("Title", null, TaskPriority.Medium, null));
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Update task validator should reject past due date.")]
    [Trait("Category", "Validators")]
    public void UpdateTaskValidator_PastDueDate_ShouldBeInvalid()
    {
        var result = _updateValidator.Validate(new UpdateTaskRequest(
            "Title", null, KanbanStatus.Todo, TaskPriority.Medium, DateTimeOffset.UtcNow.AddDays(-1)));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Filter validator should reject inverted created date range.")]
    [Trait("Category", "Validators")]
    public void FilterValidator_InvertedCreatedRange_ShouldBeInvalid()
    {
        var result = _filterValidator.Validate(new TaskFilterRequest(
            null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-1), null, null, null, null));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Filter validator should reject page zero.")]
    [Trait("Category", "Validators")]
    public void FilterValidator_PageZero_ShouldBeInvalid()
    {
        var result = _filterValidator.Validate(new TaskFilterRequest(null, null, null, null, null, null, 0, null));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Filter validator should reject page size above max.")]
    [Trait("Category", "Validators")]
    public void FilterValidator_PageSizeAboveMax_ShouldBeInvalid()
    {
        var result = _filterValidator.Validate(new TaskFilterRequest(
            null, null, null, null, null, null, null, TaskMapper.MaxPageSize + 1));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Register validator should reject short password.")]
    [Trait("Category", "Validators")]
    public void RegisterValidator_ShortPassword_ShouldBeInvalid()
    {
        var result = _registerValidator.Validate(new RegisterRequest("user@bla.local", "short"));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Login validator should reject invalid email.")]
    [Trait("Category", "Validators")]
    public void LoginValidator_InvalidEmail_ShouldBeInvalid()
    {
        var result = _loginValidator.Validate(new LoginRequest("not-an-email", "Demo123!"));
        Assert.False(result.IsValid);
    }
}
