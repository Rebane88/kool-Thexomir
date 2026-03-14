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
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
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
