using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BlaInterview.Integration.Tests.Config;

public class TasksAppFactory : WebApplicationFactory<TasksApiProgram>
{
    private readonly string _databasePath;

    public TasksAppFactory(string databasePath)
    {
        _databasePath = databasePath;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        IntegrationTestConfiguration.Apply(builder, _databasePath);
    }
}
