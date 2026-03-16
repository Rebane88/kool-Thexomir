using Application.Contracts;
using Application.Services.Auth;
using Application.Services.GameInitialization;
using Application.Services.Guard;
using Application.Services.Lobby;
using Application.Services.Building;
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
        return services;
    }
}
