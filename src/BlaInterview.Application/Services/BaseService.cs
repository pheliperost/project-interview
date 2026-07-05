using BlaInterview.Application.Interfaces;
using BlaInterview.Application.Notifications;
using FluentValidation;
using FluentValidation.Results;

namespace BlaInterview.Application.Services;

public abstract class BaseService
{
    private readonly INotifyer _notifyer;

    protected BaseService(INotifyer notifyer)
    {
        _notifyer = notifyer;
    }

    protected void Notify(ValidationResult validationResult)
    {
        foreach (var error in validationResult.Errors)
        {
            Notify(error.ErrorMessage);
        }
    }

    protected void Notify(string message, int statusCode = 400)
    {
        _notifyer.Handle(new Notification(message, statusCode));
    }

    protected async Task<bool> ValidateAsync<T>(
        IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (result.IsValid)
        {
            return true;
        }

        Notify(result);
        return false;
    }
}
