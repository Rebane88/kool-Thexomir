using Base.Contracts;
using Domain.Game;

namespace Application.Contracts;

public record GameGuardContext(Game Game, Kingdom Kingdom);

public interface IGameGuard
{
    Task<Result<GameGuardContext>> ValidateAsync(Guid gameId, Guid userId);
    Task<Result<GameGuardContext>> ValidateActionAsync(Guid gameId, Guid userId);
}
