using Application.Contracts;
using Application.Services.Combat.DTOs;
using Application.Services.Turn;
using Application.Services.Turn.DTOs;
using Application.Services.WinCondition.DTOs;
using Microsoft.Extensions.Logging;

namespace Application.Services.GameTimeout;

public class GameTimeoutService(
    IUnitOfWork unitOfWork,
    ITurnService turnService,
    IGameLockManager gameLockManager,
    ILogger<GameTimeoutService> logger) : IGameTimeoutService
{
    public async Task ProcessExpiredTurnsAsync(
        Func<Guid, TurnAutoSkippedDto, Task> onTurnAutoSkipped,
        Func<Guid, TurnAdvancedDto, Task> onTurnAdvanced,
        Func<Guid, PhaseChangedDto, Task> onPhaseChanged,
        Func<Guid, GameOverDto, Task> onGameOver,
        Func<BattleRoundResultDto, string, Task> onRoundResolved,
        Func<BattleResultDto, Task> onBattleResolved,
        CancellationToken ct)
    {
        var expiredGames = await unitOfWork.Games.GetInProgressGamesWithExpiredTurnsAsync();

        if (expiredGames.Count == 0) return;

        logger.LogInformation("[GameTimeout] Processing {Count} expired turn(s)", expiredGames.Count);

        foreach (var game in expiredGames)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                using var gameLock = await gameLockManager.AcquireAsync(game.Id, ct);

                var result = await turnService.AutoSkipTurnAsync(
                    game.Id,
                    onRoundResolved,
                    onBattleResolved);

                if (!result.IsSuccess)
                {
                    logger.LogWarning("[GameTimeout] AutoSkip failed for Game={GameId}: {Error}",
                        game.Id, result.Error);
                    continue;
                }

                var (turnAdvanced, autoSkipped) = result.Value!;

                // Broadcast TurnAutoSkipped first
                await onTurnAutoSkipped(game.Id, autoSkipped);

                // Broadcast TurnAdvanced
                await onTurnAdvanced(game.Id, turnAdvanced);

                // Broadcast PhaseChanged if applicable
                if (turnAdvanced.PhaseChanged)
                {
                    await onPhaseChanged(game.Id, new PhaseChangedDto
                    {
                        Phase = turnAdvanced.CurrentPhase,
                        PreviousPhase = "Action",
                        RoundNumber = turnAdvanced.RoundNumber
                    });
                }

                // Broadcast GameOver if applicable
                if (turnAdvanced.GameOver is not null)
                {
                    await onGameOver(game.Id, turnAdvanced.GameOver);
                }

                logger.LogInformation("[GameTimeout] AutoSkip complete for Game={GameId} SkippedKingdom={KingdomName}",
                    game.Id, autoSkipped.SkippedKingdomName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[GameTimeout] Error processing Game={GameId}", game.Id);
            }
        }
    }
}
