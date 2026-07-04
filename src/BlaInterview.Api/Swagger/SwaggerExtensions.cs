using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace BlaInterview.Api.Swagger;

public static class SwaggerExtensions
{
    public static IServiceCollection ConfigureSwaggerJwtBearer(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Simple Tasks API",
                Version = "v1",
                Description = "Personal Kanban task board API with JWT authentication."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Paste JWT only, or full value: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    new List<string>()
                }
            });
        });

        return services;
    }
}
