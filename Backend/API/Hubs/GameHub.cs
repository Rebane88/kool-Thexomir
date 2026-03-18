using System.Collections.Concurrent;
using Application.Contracts;
using Application.Services.GameHub;
using Application.Services.GameInitialization;
using Application.Services.WinCondition.DTOs;
using Domain.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

[Authorize]
public class GameHub(IGameInitializationService gameInitializationService, IUnitOfWork unitOfWork) : Hub<IGameClient>
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

                    // End game if it was in progress
                    var game = await unitOfWork.Games.GetGameWithKingdomsAsync(parsedGameId);
                    if (game is { Status: EGameStatus.InProgress })
                    {
                        game.Status = EGameStatus.Completed;
                        // No winner -- game abandoned
                        await unitOfWork.CommitAsync();

                        // Broadcast GameOver (nobody may be listening, but ensures clean state)
                        var dto = new GameOverDto
                        {
                            GameId = parsedGameId,
                            WinnerKingdomId = null,
                            WinConditionType = game.WinCondition.ToString(),
                            FinalScores = game.Kingdoms?
                                .Select(k => new KingdomScoreDto
                                {
                                    KingdomId = k.Id,
                                    KingdomName = k.Name,
                                    Score = 0,
                                    TilesOwned = 0,
                                    Status = k.Status.ToString(),
                                }).ToList() ?? [],
                            EliminationOrder = [],
                        };
                        await Clients.Group($"game:{parsedGameId}").GameOver(dto);
                    }
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
