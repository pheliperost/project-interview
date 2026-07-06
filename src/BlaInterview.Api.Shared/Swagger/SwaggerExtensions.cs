using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace BlaInterview.Api.Shared.Swagger;

public static class SwaggerExtensions
{
    public static IServiceCollection ConfigureSwaggerJwtBearer(
        this IServiceCollection services,
        string title,
        string description)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = title,
                Version = "v1",
                Description = description
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

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });

        return services;
    }
}
