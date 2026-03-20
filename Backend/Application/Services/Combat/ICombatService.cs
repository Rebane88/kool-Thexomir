using Application.Services.Combat.DTOs;
using Base.Contracts;

namespace Application.Services.Combat;

public interface ICombatService
{
    Task<Result<DeclareAttackResponse>> DeclareAttackAsync(Guid gameId, Guid userId, DeclareAttackRequest request);
    Task<Result<BattleSetupDto>> SelectArmiesAsync(Guid gameId, Guid userId, SelectArmiesRequest request);
    Task<Result<ArmyRevealDto>> GetArmyRevealAsync(Guid gameId, Guid userId, Guid declaredAttackId);
    Task<Result<BattleSetupDto>> SetLineupAsync(Guid gameId, Guid userId, SetLineupRequest request);
    Task<List<BattleResultDto>> ResolveBattlesAsync(Domain.Game.Game game);
    Task<bool> AreAllLineupsSetAsync(Guid gameId);
}
