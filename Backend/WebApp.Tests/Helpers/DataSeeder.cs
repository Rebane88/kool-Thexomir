using Infrastructure;
using Infrastructure.Seeding;

namespace WebApp.Tests.Helpers;

public static class DataSeeder
{
    public static void SeedData(AppDbContext ctx)
    {
        AppDataInit.SeedAppData(ctx);
    }
}
