using Application.Services.Military.DTOs;
using Base.Contracts;

namespace Application.Services.Military;

public interface IMilitaryService
{
    Task<Result<TroopsTrainedDto>> TrainTroopsAsync(Guid gameId, Guid userId, TrainTroopsRequest request);
    Task<Result<ArmyMovedDto>> MoveArmyAsync(Guid gameId, Guid userId, MoveArmyRequest request);
    Task<Result<CombatResolvedDto>> AttackAsync(Guid gameId, Guid userId, AttackRequest request);
}
