using Application.Contracts;
using Base.Contracts;
using Domain.Game;
using Microsoft.Extensions.Logging;

namespace Application.Services.Guard;

public class GameGuard(IUnitOfWork unitOfWork, ILogger<GameGuard> logger) : IGameGuard
{
    public async Task<Result<GameGuardContext>> ValidateAsync(Guid gameId, Guid userId)
    {
        var game = await unitOfWork.Games.GetByIdWithLockAsync(gameId);
        if (game is null)
            return Result<GameGuardContext>.Fail("Game not found.");

        if (game.Status != EGameStatus.InProgress)
        {
            logger.LogWarning("[Guard] Game={GameId} not in progress (Status={Status})", gameId, game.Status);
            return Result<GameGuardContext>.Fail("Game is not in progress.");
        }

        var kingdom = await unitOfWork.Kingdoms.GetKingdomByUserAndGameAsync(userId, gameId);
        if (kingdom is null)
            return Result<GameGuardContext>.Fail("You are not in this game.");

        if (kingdom.Status == EKingdomStatus.Defeated)
            return Result<GameGuardContext>.Fail("Your kingdom has been eliminated.");

        if (game.CurrentTurnKingdomId != kingdom.Id)
        {
            logger.LogDebug("[Guard] Not your turn. CurrentTurn={CurrentKingdomId} You={KingdomId} ({KingdomName})",
                game.CurrentTurnKingdomId, kingdom.Id, kingdom.Name);
            return Result<GameGuardContext>.Fail("It is not your turn.");
        }

        return Result<GameGuardContext>.Ok(new GameGuardContext(game, kingdom));
    }

    public async Task<Result<GameGuardContext>> ValidateActionAsync(Guid gameId, Guid userId)
    {
        var result = await ValidateAsync(gameId, userId);
        if (!result.IsSuccess) return result;

        var game = result.Value!.Game;

        if (TurnRules.IsTurnExpired(game.TurnDeadline))
            return Result<GameGuardContext>.Fail("Your turn has expired.");

        if (game.CurrentPhase != EGamePhase.Action)
        {
            logger.LogWarning("[Guard] Action attempted outside Action phase. Phase={Phase}", game.CurrentPhase);
            return Result<GameGuardContext>.Fail("Actions can only be performed during Action Phase.");
        }

        if (game.RemainingActionPoints is null || game.RemainingActionPoints <= 0)
        {
            logger.LogDebug("[Guard] No AP remaining. AP={AP}", game.RemainingActionPoints);
            return Result<GameGuardContext>.Fail("No action points remaining.");
        }

        game.RemainingActionPoints--;

        return result;
    }
}
