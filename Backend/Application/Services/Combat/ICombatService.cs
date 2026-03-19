using Application.Services.Combat.DTOs;
using Base.Contracts;

namespace Application.Services.Combat;

public interface ICombatService
{
    Task<Result<DeclareAttackResponse>> DeclareAttackAsync(Guid gameId, Guid userId, DeclareAttackRequest request);
    Task<List<BattleResultDto>> ResolveBattlesAsync(Domain.Game.Game game);
}
