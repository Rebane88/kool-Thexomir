using API.Hubs;
using Application.Services.GameHub;
using Application.Services.GameTimeout;
using Microsoft.AspNetCore.SignalR;

namespace API.BackgroundServices;

public class GameTimeoutBackgroundService(
    IServiceScopeFactory scopeFactory,
    IHubContext<GameHub, IGameClient> hubContext,
    ILogger<GameTimeoutBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[GameTimeoutBackgroundService] Starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var timeoutService = scope.ServiceProvider.GetRequiredService<IGameTimeoutService>();

                await timeoutService.ProcessExpiredTurnsAsync(
                    onTurnAutoSkipped: (gameId, dto) =>
                        hubContext.Clients.Group($"game:{gameId}").TurnAutoSkipped(dto),
                    onTurnAdvanced: (gameId, dto) =>
                        hubContext.Clients.Group($"game:{gameId}").TurnAdvanced(dto),
                    onPhaseChanged: (gameId, dto) =>
                        hubContext.Clients.Group($"game:{gameId}").PhaseChanged(dto),
                    onGameOver: (gameId, dto) =>
                        hubContext.Clients.Group($"game:{gameId}").GameOver(dto),
                    onRoundResolved: (round, battleId) =>
                        hubContext.Clients.All.BattleRoundResolved(round),
                    onBattleResolved: result =>
                        hubContext.Clients.All.BattleResolved(result),
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "[GameTimeoutBackgroundService] Error during processing");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }

        logger.LogInformation("[GameTimeoutBackgroundService] Stopping");
    }
}
