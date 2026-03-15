using Application.Services.Auth;
using Application.Services.Lobby;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILobbyService, LobbyService>();
        return services;
    }
}
