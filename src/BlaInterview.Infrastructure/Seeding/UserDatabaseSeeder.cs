using BlaInterview.Infrastructure.Identity;
using BlaInterview.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlaInterview.Infrastructure.Seeding;

public static class UserDatabaseSeeder
{
    public const string DemoEmail = "demo@example.local";
    public const string DemoPassword = "Demo123!";
    public const string OtherEmail = "other@example.local";
    public const string OtherPassword = "Other123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        await context.Database.MigrateAsync();

        await EnsureUserAsync(userManager, DemoEmail, DemoPassword);
        await EnsureUserAsync(userManager, OtherEmail, OtherPassword);

        if (environment.IsDevelopment())
        {
            await RestoreDemoUserAsync(userManager, logger);
        }

        logger.LogInformation("User database seeding completed.");
    }

    /// <summary>
    /// Development-only: keep the demo account usable after password resets or lockouts during local testing.
    /// Does not affect other users (e.g. registered accounts).
    /// </summary>
    internal static async Task RestoreDemoUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var user = await userManager.FindByEmailAsync(DemoEmail);
        if (user is null)
        {
            return;
        }

        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.SetLockoutEndDateAsync(user, null);

        if (await userManager.CheckPasswordAsync(user, DemoPassword))
        {
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, DemoPassword);
        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Could not restore demo password: {Errors}",
                string.Join(' ', result.Errors.Select(e => e.Description)));
            return;
        }

        logger.LogInformation("Demo user credentials restored for Development.");
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return user;
        }

        user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(' ', result.Errors.Select(e => e.Description)));
        }

        return user;
    }
}
