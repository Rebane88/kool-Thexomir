using Domain.Identity;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace API.Setup;

public static class IdentitySetupExtensions
{
    public static IServiceCollection AddAppIdentity(this IServiceCollection services)
    {
        services
            .AddIdentity<AppUser, AppRole>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddDefaultUI()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}
