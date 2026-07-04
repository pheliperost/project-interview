using BlaInterview.Application.Interfaces;
using BlaInterview.Application.Notifications;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BlaInterview.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Validators.RegisterRequestValidator>();
        return services;
    }

    public static IServiceCollection AddTasksApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Validators.CreateTaskRequestValidator>();
        services.AddScoped<INotifyer, Notifyer>();
        services.AddScoped<ITaskService, Services.TaskService>();
        return services;
    }
}
