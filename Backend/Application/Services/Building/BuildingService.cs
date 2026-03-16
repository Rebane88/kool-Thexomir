using Application.Contracts;
using Application.Services.Building.DTOs;
using Base.Contracts;
using Domain.Game;
using Domain.Resources;

namespace Application.Services.Building;

public class BuildingService(IUnitOfWork unitOfWork, IGameGuard gameGuard) : IBuildingService
{
    public async Task<Result<BuildingPlacedDto>> PlaceBuildingAsync(Guid gameId, Guid userId, PlaceBuildingRequest request)
    {
        var guardResult = await gameGuard.ValidateAsync(gameId, userId);
        if (!guardResult.IsSuccess)
            return Result<BuildingPlacedDto>.Fail(guardResult.Error!);

        var (game, kingdom) = guardResult.Value!;

        // Validate tile
        var tile = await unitOfWork.Tiles.GetByIdAsync(request.TileId);
        if (tile is null)
            return Result<BuildingPlacedDto>.Fail("Tile not found.");

        if (tile.KingdomId != kingdom.Id)
            return Result<BuildingPlacedDto>.Fail("You do not own this tile.");

        // Check one building per tile
        var existingBuildings = await unitOfWork.Buildings.GetBuildingsForKingdomAsync(kingdom.Id);
        if (existingBuildings.Any(b => b.TileId == request.TileId))
            return Result<BuildingPlacedDto>.Fail("This tile already has a building.");

        // Validate building type
        var buildingType = await unitOfWork.BuildingTypes.GetByIdAsync(request.BuildingTypeId);
        if (buildingType is null)
            return Result<BuildingPlacedDto>.Fail("Building type not found.");

        // Check prerequisite chain
        if (buildingType.PrerequisiteBuildingTypeId.HasValue)
        {
            var hasPrerequisite = existingBuildings.Any(b => b.BuildingTypeId == buildingType.PrerequisiteBuildingTypeId.Value);
            if (!hasPrerequisite)
                return Result<BuildingPlacedDto>.Fail("Missing prerequisite building. Build the required lower-tier building first.");
        }

        // Calculate costs with faction modifier
        var faction = await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId!.Value);
        var costModifier = faction?.BuildingCostModifier ?? 1.0m;

        var costs = new Dictionary<EResourceType, int>
        {
            { EResourceType.Gold, (int)Math.Floor(buildingType.GoldCost * costModifier) },
            { EResourceType.Wood, (int)Math.Floor(buildingType.WoodCost * costModifier) },
            { EResourceType.Stone, (int)Math.Floor(buildingType.StoneCost * costModifier) },
            { EResourceType.Mana, (int)Math.Floor(buildingType.ManaCost * costModifier) },
        };

        // Load tracked resources for atomic deduction
        var resources = await unitOfWork.KingdomResources.GetMutableResourcesForKingdomAsync(kingdom.Id);

        // Check ALL costs first (all-or-nothing)
        foreach (var (type, cost) in costs.Where(c => c.Value > 0))
        {
            var resource = resources.SingleOrDefault(r => r.ResourceType == type);
            if (resource is null || resource.Amount < cost)
                return Result<BuildingPlacedDto>.Fail($"Not enough {type}. Need {cost}, have {(int)(resource?.Amount ?? 0)}.");
        }

        // Deduct ALL costs
        foreach (var (type, cost) in costs.Where(c => c.Value > 0))
        {
            var resource = resources.Single(r => r.ResourceType == type);
            resource.Amount -= cost;
            await unitOfWork.KingdomResources.UpdateAsync(resource);
        }

        // Create building
        var building = new Domain.Buildings.Building
        {
            TileId = request.TileId,
            BuildingTypeId = request.BuildingTypeId,
        };
        await unitOfWork.Buildings.AddAsync(building);

        // Log the build action
        await unitOfWork.TurnLogs.AddAsync(new TurnLog
        {
            GameId = gameId,
            KingdomId = kingdom.Id,
            TurnNumber = game.TurnNumber,
            Action = "Build"
        });

        await unitOfWork.CommitAsync();

        return Result<BuildingPlacedDto>.Ok(new BuildingPlacedDto
        {
            BuildingId = building.Id,
            TileId = request.TileId,
            BuildingTypeId = request.BuildingTypeId,
            BuildingName = buildingType.Name.Translate() ?? string.Empty,
            KingdomId = kingdom.Id,
            ResourcesAfter = resources.ToDictionary(r => r.ResourceType.ToString(), r => (int)r.Amount)
        });
    }
}
