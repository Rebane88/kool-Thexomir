using Application.Services.Army.DTOs;
using Base.Contracts;

namespace Application.Services.Army;

public interface IArmyService
{
    Task<Result<ArmyTrainedDto>> TrainArmyAsync(Guid gameId, Guid userId, TrainArmyRequest request);
    Task<IEnumerable<ArmyTypeDto>> GetArmyTypesAsync(Guid gameId, Guid userId);
}
