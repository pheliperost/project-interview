using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BlaInterview.Integration.Tests.Config;

public static class IntegrationTestConfiguration
{
    public const string JwtSecret = "SimpleTasksDevSecretKey_ChangeInProduction_32chars!";

    public static Dictionary<string, string?> ForDatabase(string databasePath) => new()
    {
        ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath}",
        ["Jwt:Secret"] = JwtSecret,
        ["Jwt:Issuer"] = "SimpleTasks",
        ["Jwt:Audience"] = "SimpleTasks",
        ["Jwt:ExpiryMinutes"] = "60",
        ["PasswordReset:ClientBaseUrl"] = "http://localhost:5173"
    };

    public static void Apply(IWebHostBuilder builder, string databasePath)
    {
        builder.UseEnvironment("Testing");

        foreach (var (key, value) in ForDatabase(databasePath))
        {
            if (value is not null)
            {
                builder.UseSetting(key, value);
            }
        }

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(ForDatabase(databasePath));
        });
    }
}

public class AuthAppFactory : WebApplicationFactory<AuthApiProgram>
{
    private readonly string _databasePath;

    public AuthAppFactory(string databasePath)
    {
        _databasePath = databasePath;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        IntegrationTestConfiguration.Apply(builder, _databasePath);
    }
}
