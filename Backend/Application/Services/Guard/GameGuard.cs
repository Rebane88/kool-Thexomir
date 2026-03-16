using Application.Contracts;
using Base.Contracts;
using Domain.Game;

namespace Application.Services.Guard;

public class GameGuard(IUnitOfWork unitOfWork) : IGameGuard
{
    public async Task<Result<GameGuardContext>> ValidateAsync(Guid gameId, Guid userId)
    {
        var game = await unitOfWork.Games.GetByIdForUpdateAsync(gameId);
        if (game is null)
            return Result<GameGuardContext>.Fail("Game not found.");

        if (game.Status != EGameStatus.InProgress)
            return Result<GameGuardContext>.Fail("Game is not in progress.");

        var kingdom = await unitOfWork.Kingdoms.GetKingdomByUserAndGameAsync(userId, gameId);
        if (kingdom is null)
            return Result<GameGuardContext>.Fail("You are not in this game.");

        if (kingdom.IsEliminated)
            return Result<GameGuardContext>.Fail("Your kingdom has been eliminated.");

        if (game.CurrentTurnKingdomId != kingdom.Id)
            return Result<GameGuardContext>.Fail("It is not your turn.");

        return Result<GameGuardContext>.Ok(new GameGuardContext(game, kingdom));
    }
}
