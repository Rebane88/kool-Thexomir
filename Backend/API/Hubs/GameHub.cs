using System.Collections.Concurrent;
using Application.Services.GameHub;
using Application.Services.GameInitialization;
using Domain.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

[Authorize(Policy = "PublicOrJwtPolicy")]
public class GameHub(IGameInitializationService gameInitializationService) : Hub<IGameClient>
{
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> GameConnections = new();

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var gameId = httpContext?.Request.Query["gameId"].ToString();

        if (!string.IsNullOrEmpty(gameId) && Guid.TryParse(gameId, out var parsedGameId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"game:{parsedGameId}");

            var connections = GameConnections.GetOrAdd(parsedGameId, _ => new ConcurrentDictionary<string, byte>());
            connections.TryAdd(Context.ConnectionId, 0);

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
        var httpContext = Context.GetHttpContext();
        var gameId = httpContext?.Request.Query["gameId"].ToString();

        if (!string.IsNullOrEmpty(gameId) && Guid.TryParse(gameId, out var parsedGameId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game:{parsedGameId}");

            if (GameConnections.TryGetValue(parsedGameId, out var connections))
            {
                connections.TryRemove(Context.ConnectionId, out _);

                if (connections.IsEmpty)
                {
                    GameConnections.TryRemove(parsedGameId, out _);
                    // Disconnection does NOT terminate the game — the background timeout service handles
                    // game lifecycle. Players can reconnect and the game continues.
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
