using System.Text;
using Application.Contracts;
using Domain.Buildings;
using Domain.Factions;
using Domain.Game;
using Domain.Identity;
using Domain.Map;
using Domain.Military;
using Domain.Resources;
using Infrastructure.Identity;
using Infrastructure.Repositories.Buildings;
using Infrastructure.Repositories.Factions;
using Infrastructure.Repositories.Game;
using Infrastructure.Repositories.Identity;
using Infrastructure.Repositories.Map;
using Infrastructure.Repositories.Military;
using Infrastructure.Repositories.Resources;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Identity — must be registered before AddAuthentication to avoid scheme conflicts
        services
            .AddIdentity<AppUser, AppRole>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // JWT Bearer authentication — overrides cookie scheme set by AddIdentity
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["JWT:Key"]!)),
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidateIssuer = true,
                    ValidAudience = configuration["JWT:Audience"],
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Identity abstraction
        services.AddScoped<IIdentityService, IdentityService>();

        // Repositories - Identity
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IAppRefreshTokenRepository, AppRefreshTokenRepository>();

        // Repositories - Game
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IKingdomRepository, KingdomRepository>();
        services.AddScoped<ITurnLogRepository, TurnLogRepository>();
        services.AddScoped<IGameEventRepository, GameEventRepository>();

        // Repositories - Map
        services.AddScoped<ITileRepository, TileRepository>();
        services.AddScoped<ITerrainTypeRepository, TerrainTypeRepository>();

        // Repositories - Buildings
        services.AddScoped<IBuildingRepository, BuildingRepository>();
        services.AddScoped<IBuildingTypeRepository, BuildingTypeRepository>();

        // Repositories - Military
        services.AddScoped<IArmyRepository, ArmyRepository>();
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<IUnitTypeRepository, UnitTypeRepository>();
        services.AddScoped<IUnitTypeMatchupRepository, UnitTypeMatchupRepository>();
        services.AddScoped<IBattleRepository, BattleRepository>();

        // Repositories - Resources
        services.AddScoped<IKingdomResourceRepository, KingdomResourceRepository>();

        // Repositories - Factions
        services.AddScoped<IFactionTypeRepository, FactionTypeRepository>();
        services.AddScoped<IFactionResourceBonusRepository, FactionResourceBonusRepository>();
        services.AddScoped<IFactionUnitBonusRepository, FactionUnitBonusRepository>();

        return services;
    }
}
