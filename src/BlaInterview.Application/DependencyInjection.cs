using BlaInterview.Application;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BlaInterview.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Validators.RegisterRequestValidator>();
        services.AddScoped<Interfaces.ITaskService, Services.TaskService>();
        return services;
    }
}
