using System.Threading;
using Domain.Identity;
using Infrastructure;
using Infrastructure.Seeding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace API.Setup;

public static class AppDataInitExtensions
{
    public static void SetupAppData(this WebApplication app)
    {
        using var serviceScope = app.Services
            .GetRequiredService<IServiceScopeFactory>()
            .CreateScope();
        var logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<IApplicationBuilder>>();

        using var context = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory") return;

        WaitDbConnection(context, logger);

        using var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        using var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

        var configuration = app.Configuration;

        if (configuration.GetValue<bool>("DataInitialization:DropDatabase"))
        {
            logger.LogWarning("DropDatabase");
            AppDataInit.DeleteDatabase(context);
        }

        if (configuration.GetValue<bool>("DataInitialization:MigrateDatabase"))
        {
            logger.LogInformation("MigrateDatabase");
            AppDataInit.MigrateDatabase(context);
        }

        if (configuration.GetValue<bool>("DataInitialization:SeedIdentity"))
        {
            logger.LogInformation("SeedIdentity");
            AppDataInit.SeedIdentity(userManager, roleManager);
        }

        if (configuration.GetValue<bool>("DataInitialization:SeedData"))
        {
            logger.LogInformation("SeedData");
            AppDataInit.SeedAppData(context);
        }
    }

    private static void WaitDbConnection(AppDbContext ctx, ILogger<IApplicationBuilder> logger)
    {
        while (true)
        {
            try
            {
                ctx.Database.OpenConnection();
                ctx.Database.CloseConnection();
                return;
            }
            catch (Npgsql.PostgresException e)
            {
                logger.LogWarning("Checked postgres db connection. Got: {}", e.Message);

                if (e.Message.Contains("does not exist"))
                {
                    logger.LogWarning("Applying migration, probably db is not there (but server is)");
                    return;
                }

                logger.LogWarning("Waiting for db connection. Sleep 1 sec");
                Thread.Sleep(1000);
            }
        }
    }
}
