using Application.Contracts;
using Application.Services.Turn.DTOs;
using Base.Contracts;
using Domain.Game;
using Domain.Map;
using Domain.Resources;

namespace Application.Services.Turn;

public class TurnService(IUnitOfWork unitOfWork, IGameGuard gameGuard) : ITurnService
{
    public async Task<Result<TurnAdvancedDto>> EndTurnAsync(Guid gameId, Guid userId)
    {
        var guardResult = await gameGuard.ValidateAsync(gameId, userId);
        if (!guardResult.IsSuccess)
            return Result<TurnAdvancedDto>.Fail(guardResult.Error!);

        var (game, currentKingdom) = guardResult.Value!;

        // Log the end-turn action with CURRENT turn number before any changes
        await unitOfWork.TurnLogs.AddAsync(new TurnLog
        {
            GameId = gameId,
            KingdomId = currentKingdom.Id,
            TurnNumber = game.TurnNumber,
            Action = "EndTurn"
        });

        // Get all non-eliminated kingdoms ordered by join order
        var kingdoms = (await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId))
            .Where(k => !k.IsEliminated)
            .OrderBy(k => k.CreatedAt)
            .ThenBy(k => k.Id)
            .ToList();

        // Find next kingdom in round-robin
        var currentIndex = kingdoms.FindIndex(k => k.Id == currentKingdom.Id);
        var nextIndex = (currentIndex + 1) % kingdoms.Count;
        var nextKingdom = kingdoms[nextIndex];

        // Increment turn number if wrapping back to first player
        if (nextIndex <= currentIndex)
            game.TurnNumber++;

        game.CurrentTurnKingdomId = nextKingdom.Id;
        await unitOfWork.Games.UpdateAsync(game);

        // Calculate and apply income for the next player
        var income = await CalculateIncomeAsync(nextKingdom);
        var resources = await unitOfWork.KingdomResources.GetResourcesForKingdomTrackedAsync(nextKingdom.Id);
        foreach (var (type, amount) in income.Where(i => i.Value > 0))
        {
            var resource = resources.Single(r => r.ResourceType == type);
            resource.Amount += amount;
            await unitOfWork.KingdomResources.UpdateAsync(resource);
        }

        await unitOfWork.CommitAsync();

        return Result<TurnAdvancedDto>.Ok(new TurnAdvancedDto
        {
            NewKingdomId = nextKingdom.Id,
            TurnNumber = game.TurnNumber,
            IncomeApplied = income.Where(i => i.Value > 0).ToDictionary(i => i.Key.ToString(), i => i.Value)
        });
    }

    private async Task<Dictionary<EResourceType, int>> CalculateIncomeAsync(Kingdom kingdom)
    {
        var tiles = await unitOfWork.Tiles.GetTilesWithBuildingsAndTerrainForKingdomAsync(kingdom.Id);

        var factionBonuses = await unitOfWork.FactionResourceBonuses
            .GetBonusesForFactionAsync(kingdom.FactionTypeId!.Value);
        var bonusLookup = factionBonuses.ToDictionary(b => b.ResourceType, b => b.Multiplier);

        var income = new Dictionary<EResourceType, int>
        {
            { EResourceType.Gold, 0 },
            { EResourceType.Food, 0 },
            { EResourceType.Wood, 0 },
            { EResourceType.Stone, 0 },
            { EResourceType.Mana, 0 },
        };

        foreach (var tile in tiles)
        {
            if (tile.Buildings is null) continue;
            foreach (var building in tile.Buildings)
            {
                var bt = building.BuildingType!;
                var terrainBonus = tile.TerrainType!.ResourceBonusType;

                AddYield(income, EResourceType.Food, bt.FoodYield, terrainBonus, bonusLookup);
                AddYield(income, EResourceType.Wood, bt.WoodYield, terrainBonus, bonusLookup);
                AddYield(income, EResourceType.Stone, bt.StoneYield, terrainBonus, bonusLookup);
                AddYield(income, EResourceType.Gold, bt.GoldYield, terrainBonus, bonusLookup);
                AddYield(income, EResourceType.Mana, bt.ManaYield, terrainBonus, bonusLookup);
            }
        }

        return income;
    }

    private static void AddYield(
        Dictionary<EResourceType, int> income,
        EResourceType resourceType,
        int baseYield,
        ETerrainResourceBonus terrainBonus,
        Dictionary<EResourceType, decimal> factionBonuses)
    {
        if (baseYield <= 0) return;

        decimal terrainMultiplier = MapTerrainToResource(terrainBonus) == resourceType ? 1.25m : 1.00m;
        decimal factionMultiplier = factionBonuses.TryGetValue(resourceType, out var fm) ? fm : 1.00m;

        int contribution = (int)Math.Floor(baseYield * terrainMultiplier * factionMultiplier);
        income[resourceType] += contribution;
    }

    private static EResourceType? MapTerrainToResource(ETerrainResourceBonus terrain) => terrain switch
    {
        ETerrainResourceBonus.Food => EResourceType.Food,
        ETerrainResourceBonus.Wood => EResourceType.Wood,
        ETerrainResourceBonus.Stone => EResourceType.Stone,
        ETerrainResourceBonus.Gold => EResourceType.Gold,
        ETerrainResourceBonus.Mana => EResourceType.Mana,
        _ => null,
    };
}
