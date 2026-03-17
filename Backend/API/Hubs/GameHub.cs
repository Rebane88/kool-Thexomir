using Application.Services.GameHub;
using Application.Services.GameInitialization;
using Domain.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

[Authorize]
public class GameHub(IGameInitializationService gameInitializationService) : Hub<IGameClient>
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var gameId = httpContext?.Request.Query["gameId"].ToString();

        if (!string.IsNullOrEmpty(gameId) && Guid.TryParse(gameId, out var parsedGameId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{parsedGameId}");

            // If the game is in progress, send current state snapshot to reconnecting player
            try
            {
                var gameState = await gameInitializationService.BuildGameStateSnapshotAsync(parsedGameId);
                if (gameState.Status == EGameStatus.InProgress.ToString())
                {
                    await Clients.Caller.GameStateSnapshot(gameState);
                }
            }
            catch
            {
                // Game may not exist yet (lobby phase) or may have been deleted -- no snapshot needed
            }
        }

        await base.OnConnectedAsync();
    }

    public async Task RequestGameSnapshot()
    {
        var httpContext = Context.GetHttpContext();
        var gameId = httpContext?.Request.Query["gameId"].ToString();

        if (!string.IsNullOrEmpty(gameId) && Guid.TryParse(gameId, out var parsedGameId))
        {
            var gameState = await gameInitializationService.BuildGameStateSnapshotAsync(parsedGameId);
            await Clients.Caller.GameStateSnapshot(gameState);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
