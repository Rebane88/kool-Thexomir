using API.Hubs;

namespace API.Setup;

public static class SignalRExtensions
{
    public static IServiceCollection AddAppSignalR(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }

    public static WebApplication MapAppHubs(this WebApplication app)
    {
        app.MapHub<GameHub>("/hubs/game");
        return app;
    }
}
