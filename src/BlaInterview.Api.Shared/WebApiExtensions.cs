using System.Text.Json.Serialization;
using BlaInterview.Api.Shared.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace BlaInterview.Api.Shared;

public static class WebApiExtensions
{
    public static IServiceCollection AddSharedWebApi(this IServiceCollection services)
    {
        services.AddExceptionHandler<AppExceptionHandler>();
        services.AddProblemDetails();

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddCors(options =>
        {
            options.AddPolicy("Client", policy =>
                policy.WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });

        services.AddEndpointsApiExplorer();
        return services;
    }

    public static WebApplication UseSharedWebApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseHttpsRedirection();
        app.UseCors("Client");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }
}
