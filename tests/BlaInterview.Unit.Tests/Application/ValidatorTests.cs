using BlaInterview.Application.DTOs;
using BlaInterview.Application.Mapping;
using BlaInterview.Application.Validators;
using BlaInterview.Domain.Enums;

namespace BlaInterview.Unit.Tests.Application;

public class ValidatorTests
{
    private readonly CreateTaskBodyValidator _createValidator = new();
    private readonly UpdateTaskBodyValidator _updateValidator = new();
    private readonly TaskFilterRequestValidator _filterValidator = new();
    private readonly RegisterRequestValidator _registerValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();
    private readonly ForgotPasswordRequestValidator _forgotPasswordValidator = new();
    private readonly ResetPasswordRequestValidator _resetPasswordValidator = new();

    [Fact(DisplayName = "Create task validator should reject empty title.")]
    [Trait("Category", "Validators")]
    public void CreateTaskValidator_EmptyTitle_ShouldBeInvalid()
    {
        var result = _createValidator.Validate(new CreateTaskBody("", null, TaskPriority.Medium, null));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Create task validator should reject past due date.")]
    [Trait("Category", "Validators")]
    public void CreateTaskValidator_PastDueDate_ShouldBeInvalid()
    {
        var result = _createValidator.Validate(new CreateTaskBody("Title", null, TaskPriority.Medium, DateTimeOffset.UtcNow.AddDays(-1)));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Create task validator should accept null due date.")]
    [Trait("Category", "Validators")]
    public void CreateTaskValidator_NullDueDate_ShouldBeValid()
    {
        var result = _createValidator.Validate(new CreateTaskBody("Title", "A description", TaskPriority.Medium, null));
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Create task validator should reject empty description.")]
    [Trait("Category", "Validators")]
    public void CreateTaskValidator_EmptyDescription_ShouldBeInvalid()
    {
        var result = _createValidator.Validate(new CreateTaskBody("Title", "", TaskPriority.Medium, null));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Update task validator should accept past due date in payload.")]
    [Trait("Category", "Validators")]
    public void UpdateTaskValidator_PastDueDate_ShouldBeValid()
    {
        var result = _updateValidator.Validate(new UpdateTaskBody(
            "Title", "A description", KanbanStatus.Todo, TaskPriority.Medium, DateTimeOffset.UtcNow.AddDays(-1)));
        Assert.True(result.IsValid);
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
            null, null, null, null, null, null, null, TaskPagination.MaxPageSize + 1));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Register validator should reject short password.")]
    [Trait("Category", "Validators")]
    public void RegisterValidator_ShortPassword_ShouldBeInvalid()
    {
        var result = _registerValidator.Validate(new RegisterRequest("user@example.local", "short"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("8 characters", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Register validator should reject password missing uppercase.")]
    [Trait("Category", "Validators")]
    public void RegisterValidator_PasswordMissingUppercase_ShouldBeInvalid()
    {
        var result = _registerValidator.Validate(new RegisterRequest("user@example.local", "test1234!"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("uppercase", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Register validator should reject invalid email.")]
    [Trait("Category", "Validators")]
    public void RegisterValidator_InvalidEmail_ShouldBeInvalid()
    {
        var result = _registerValidator.Validate(new RegisterRequest("not-an-email", "Test1234!"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("valid email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Register validator should accept valid credentials.")]
    [Trait("Category", "Validators")]
    public void RegisterValidator_ValidRequest_ShouldBeValid()
    {
        var result = _registerValidator.Validate(new RegisterRequest("user@example.local", "Test1234!"));
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Login validator should reject invalid email.")]
    [Trait("Category", "Validators")]
    public void LoginValidator_InvalidEmail_ShouldBeInvalid()
    {
        var result = _loginValidator.Validate(new LoginRequest("not-an-email", "Demo123!"));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Login validator should accept valid credentials.")]
    [Trait("Category", "Validators")]
    public void LoginValidator_ValidRequest_ShouldBeValid()
    {
        var result = _loginValidator.Validate(new LoginRequest("user@example.local", "Demo123!"));
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Forgot password validator should reject invalid email.")]
    [Trait("Category", "Validators")]
    public void ForgotPasswordValidator_InvalidEmail_ShouldBeInvalid()
    {
        var result = _forgotPasswordValidator.Validate(new ForgotPasswordRequest("not-an-email"));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Reset password validator should reject short password.")]
    [Trait("Category", "Validators")]
    public void ResetPasswordValidator_ShortPassword_ShouldBeInvalid()
    {
        var result = _resetPasswordValidator.Validate(new ResetPasswordRequest("user@example.local", "token", "short"));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Forgot password validator should accept valid email.")]
    [Trait("Category", "Validators")]
    public void ForgotPasswordValidator_ValidEmail_ShouldBeValid()
    {
        var result = _forgotPasswordValidator.Validate(new ForgotPasswordRequest("user@example.local"));
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Reset password validator should accept valid request.")]
    [Trait("Category", "Validators")]
    public void ResetPasswordValidator_ValidRequest_ShouldBeValid()
    {
        var result = _resetPasswordValidator.Validate(new ResetPasswordRequest("user@example.local", "token", "NewPass1!"));
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Filter validator should accept valid pagination.")]
    [Trait("Category", "Validators")]
    public void FilterValidator_ValidPagination_ShouldBeValid()
    {
        var result = _filterValidator.Validate(new TaskFilterRequest(null, null, null, null, null, null, 1, 50));
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Create task validator should accept valid request.")]
    [Trait("Category", "Validators")]
    public void CreateTaskValidator_ValidRequest_ShouldBeValid()
    {
        var result = _createValidator.Validate(new CreateTaskBody(
            "Valid title",
            "Valid description",
            TaskPriority.High,
            DateTimeOffset.UtcNow.AddDays(1)));
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Update task validator should reject empty title.")]
    [Trait("Category", "Validators")]
    public void UpdateTaskValidator_EmptyTitle_ShouldBeInvalid()
    {
        var result = _updateValidator.Validate(new UpdateTaskBody(
            "", "Description", KanbanStatus.Todo, TaskPriority.Medium, null));
        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Update task validator should accept valid request.")]
    [Trait("Category", "Validators")]
    public void UpdateTaskValidator_ValidRequest_ShouldBeValid()
    {
        var result = _updateValidator.Validate(new UpdateTaskBody(
            "Title", "Description", KanbanStatus.InProgress, TaskPriority.High, null));
        Assert.True(result.IsValid);
    }
}
