using BlaInterview.Infrastructure.Identity;
using BlaInterview.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlaInterview.Infrastructure.Seeding;

public static class UserDatabaseSeeder
{
    public const string DemoEmail = "demo@bla.local";
    public const string DemoPassword = "Demo123!";
    public const string OtherEmail = "other@bla.local";
    public const string OtherPassword = "Other123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        await context.Database.MigrateAsync();

        await EnsureUserAsync(userManager, DemoEmail, DemoPassword);
        await EnsureUserAsync(userManager, OtherEmail, OtherPassword);

        logger.LogInformation("User database seeding completed.");
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
            return user;

        user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(' ', result.Errors.Select(e => e.Description)));

        return user;
    }
}
