using Application.Contracts;
using Application.Services.Abandon;
using Application.Services.Auth;
using Application.Services.GameInitialization;
using Application.Services.GameTimeout;
using Application.Services.Guard;
using Application.Services.Lobby;
using Application.Services.Building;
using Application.Services.Army;
using Application.Services.Combat;
using Application.Services.SlotMachine;
using Application.Services.Turn;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILobbyService, LobbyService>();
        services.AddScoped<IGameInitializationService, GameInitializationService>();
        services.AddScoped<IGameGuard, GameGuard>();
        services.AddScoped<ITurnService, TurnService>();
        services.AddScoped<IBuildingService, BuildingService>();
        services.AddScoped<ISlotMachineService, SlotMachineService>();
        services.AddScoped<IArmyService, ArmyService>();
        services.AddScoped<ICombatService, CombatService>();
        services.AddScoped<IGameTimeoutService, GameTimeoutService>();
        services.AddScoped<IAbandonService, AbandonService>();
        return services;
    }
}
