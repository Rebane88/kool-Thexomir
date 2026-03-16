using Application.Services.Building.DTOs;
using Base.Contracts;

namespace Application.Services.Building;

public interface IBuildingService
{
    Task<Result<BuildingPlacedDto>> PlaceBuildingAsync(Guid gameId, Guid userId, PlaceBuildingRequest request);
}
