using Application.Contracts;
using Application.Services.Building.DTOs;
using Base.Contracts;
using Domain.Game;
using Domain.Resources;

namespace Application.Services.Building;

public class BuildingService(IUnitOfWork unitOfWork, IGameGuard gameGuard) : IBuildingService
{
    public async Task<IEnumerable<BuildingTypeDto>> GetBuildingTypesAsync()
    {
        var buildingTypes = await unitOfWork.BuildingTypes.GetAllWithPrerequisiteAsync();

        return buildingTypes.Select(bt => new BuildingTypeDto
        {
            Id = bt.Id,
            Name = bt.Name.Translate() ?? string.Empty,
            Tier = bt.Tier,
            Chain = bt.Chain,
            GoldCost = bt.GoldCost,
            WoodCost = bt.WoodCost,
            StoneCost = bt.StoneCost,
            ManaCost = bt.ManaCost,
            FoodYield = bt.FoodYield,
            WoodYield = bt.WoodYield,
            StoneYield = bt.StoneYield,
            GoldYield = bt.GoldYield,
            ManaYield = bt.ManaYield,
            Description = bt.Description,
            PrerequisiteBuildingTypeId = bt.PrerequisiteBuildingTypeId,
            PrerequisiteBuildingName = bt.PrerequisiteBuildingType?.Name.Translate()
        });
    }

    public async Task<Result<BuildingPlacedDto>> PlaceBuildingAsync(Guid gameId, Guid userId, PlaceBuildingRequest request)
    {
        var guardResult = await gameGuard.ValidateAsync(gameId, userId);
        if (!guardResult.IsSuccess)
            return Result<BuildingPlacedDto>.Fail(guardResult.Error!);

        var (game, kingdom) = guardResult.Value!;

        // Validate tile exists
        var tile = await unitOfWork.Tiles.GetByIdAsync(request.TileId);
        if (tile is null)
            return Result<BuildingPlacedDto>.Fail("Tile not found.");

        // Validate building type exists
        var buildingType = await unitOfWork.BuildingTypes.GetByIdAsync(request.BuildingTypeId);
        if (buildingType is null)
            return Result<BuildingPlacedDto>.Fail("Building type not found.");

        // Load data for domain method
        var existingBuildings = await unitOfWork.Buildings.GetBuildingsForKingdomAsync(kingdom.Id);
        var resources = await unitOfWork.KingdomResources.GetMutableResourcesForKingdomAsync(kingdom.Id);

        var faction = await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId!.Value);
        var costModifier = faction?.BuildingCostModifier ?? 1.0m;

        // Ensure game has kingdoms loaded for PlaceBuilding
        game.Kingdoms = [kingdom];

        // Domain does the validation and resource deduction
        var result = game.PlaceBuilding(request.TileId, buildingType, tile, existingBuildings, resources, costModifier);
        if (!result.IsSuccess)
            return Result<BuildingPlacedDto>.Fail(result.Error!);

        var building = result.Value!;
        await unitOfWork.Buildings.AddAsync(building);

        // Persist resource changes (resources were mutated by domain method)
        foreach (var resource in resources)
            await unitOfWork.KingdomResources.UpdateAsync(resource);

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
