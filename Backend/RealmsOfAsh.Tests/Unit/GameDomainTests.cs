using Base.Contracts;
using Domain.Buildings;
using Domain.Game;
using Domain.Map;
using Domain.Resources;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// Pure domain-level unit tests for Game aggregate behavior.
/// No mocks needed -- tests create entities directly and call domain methods.
/// </summary>
public class GameDomainTests
{
    private static readonly Guid Kingdom1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Kingdom2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Kingdom3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static Game CreateGameWithKingdoms(
        Guid currentTurnKingdomId,
        int turnNumber = 1,
        Guid? eliminatedId = null)
    {
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Status = EGameStatus.InProgress,
            TurnNumber = turnNumber,
            CurrentTurnKingdomId = currentTurnKingdomId,
            Kingdoms = new List<Kingdom>
            {
                new()
                {
                    Id = Kingdom1Id, IsEliminated = eliminatedId == Kingdom1Id,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    Id = Kingdom2Id, IsEliminated = eliminatedId == Kingdom2Id,
                    CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    Id = Kingdom3Id, IsEliminated = eliminatedId == Kingdom3Id,
                    CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                },
            },
        };
        return game;
    }

    // -------------------------------------------------------------------------
    // AdvanceTurn tests
    // -------------------------------------------------------------------------

    [Fact]
    public void AdvanceTurn_CurrentIsFirst_AdvancesToSecond()
    {
        var game = CreateGameWithKingdoms(Kingdom1Id);

        var next = game.AdvanceTurn();

        next.Id.ShouldBe(Kingdom2Id);
        game.CurrentTurnKingdomId.ShouldBe(Kingdom2Id);
        game.TurnNumber.ShouldBe(1); // no wrap
    }

    [Fact]
    public void AdvanceTurn_CurrentIsLast_WrapsToFirstAndIncrementsTurn()
    {
        var game = CreateGameWithKingdoms(Kingdom3Id, turnNumber: 1);

        var next = game.AdvanceTurn();

        next.Id.ShouldBe(Kingdom1Id);
        game.CurrentTurnKingdomId.ShouldBe(Kingdom1Id);
        game.TurnNumber.ShouldBe(2); // wrapped
    }

    [Fact]
    public void AdvanceTurn_MiddleEliminated_SkipsToThird()
    {
        var game = CreateGameWithKingdoms(Kingdom1Id, eliminatedId: Kingdom2Id);

        var next = game.AdvanceTurn();

        next.Id.ShouldBe(Kingdom3Id);
        game.CurrentTurnKingdomId.ShouldBe(Kingdom3Id);
        game.TurnNumber.ShouldBe(1); // no wrap
    }

    [Fact]
    public void AdvanceTurn_OnlyOneActive_StaysOnSameKingdom()
    {
        // Two kingdoms eliminated, only Kingdom2 is active
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Status = EGameStatus.InProgress,
            TurnNumber = 1,
            CurrentTurnKingdomId = Kingdom2Id,
            Kingdoms = new List<Kingdom>
            {
                new()
                {
                    Id = Kingdom1Id, IsEliminated = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    Id = Kingdom2Id, IsEliminated = false,
                    CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    Id = Kingdom3Id, IsEliminated = true,
                    CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                },
            },
        };

        var next = game.AdvanceTurn();

        next.Id.ShouldBe(Kingdom2Id);
        game.CurrentTurnKingdomId.ShouldBe(Kingdom2Id);
        game.TurnNumber.ShouldBe(2); // wraps to same = increment
    }

    // -------------------------------------------------------------------------
    // CalculateIncome tests
    // -------------------------------------------------------------------------

    [Fact]
    public void CalculateIncome_BuildingOnMatchingTerrain_AppliesTerrainBonus()
    {
        // Capital: 5 food, 2 wood, 2 stone, 5 gold, 1 mana on Plains (food bonus)
        var tiles = new List<Tile>
        {
            new()
            {
                TerrainType = new TerrainType { ResourceBonusType = ETerrainResourceBonus.Food },
                Buildings = new List<Building>
                {
                    new()
                    {
                        BuildingType = new BuildingType
                        {
                            FoodYield = 5, WoodYield = 2, StoneYield = 2,
                            GoldYield = 5, ManaYield = 1,
                        },
                    },
                },
            },
        };

        var income = Game.CalculateIncome(tiles, new Dictionary<EResourceType, decimal>());

        // Food: floor(5 * 1.25) = 6, others at 1.0x
        income[EResourceType.Food].ShouldBe(6);
        income[EResourceType.Wood].ShouldBe(2);
        income[EResourceType.Stone].ShouldBe(2);
        income[EResourceType.Gold].ShouldBe(5);
        income[EResourceType.Mana].ShouldBe(1);
    }

    [Fact]
    public void CalculateIncome_TerrainAndFactionBonusStacked()
    {
        // Building with WoodYield=10, terrain Wood bonus (1.25x), faction 1.2 for Wood
        var tiles = new List<Tile>
        {
            new()
            {
                TerrainType = new TerrainType { ResourceBonusType = ETerrainResourceBonus.Wood },
                Buildings = new List<Building>
                {
                    new()
                    {
                        BuildingType = new BuildingType { WoodYield = 10 },
                    },
                },
            },
        };
        var factionBonuses = new Dictionary<EResourceType, decimal>
        {
            { EResourceType.Wood, 1.2m },
        };

        var income = Game.CalculateIncome(tiles, factionBonuses);

        // floor(10 * 1.25 * 1.2) = floor(15.0) = 15
        income[EResourceType.Wood].ShouldBe(15);
    }

    [Fact]
    public void CalculateIncome_ZeroBuildings_AllZero()
    {
        var income = Game.CalculateIncome(
            Enumerable.Empty<Tile>(),
            new Dictionary<EResourceType, decimal>());

        income[EResourceType.Gold].ShouldBe(0);
        income[EResourceType.Food].ShouldBe(0);
        income[EResourceType.Wood].ShouldBe(0);
        income[EResourceType.Stone].ShouldBe(0);
        income[EResourceType.Mana].ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // PlaceBuilding tests
    // -------------------------------------------------------------------------

    private static readonly Guid TileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid BuildingTypeId = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000001");
    private static readonly Guid PrereqBuildingTypeId = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000100");

    private static Game CreateGameForPlacement(Guid kingdomId)
    {
        return new Game
        {
            Id = Guid.NewGuid(),
            Status = EGameStatus.InProgress,
            TurnNumber = 1,
            CurrentTurnKingdomId = kingdomId,
            Kingdoms = new List<Kingdom>
            {
                new() { Id = kingdomId, CreatedAt = DateTime.UtcNow },
            },
        };
    }

    private static Tile CreateOwnedTile(Guid kingdomId) => new()
    {
        Id = TileId,
        KingdomId = kingdomId,
    };

    private static BuildingType CreateBuildingType(int goldCost = 100, int woodCost = 0, int stoneCost = 0, int manaCost = 0) => new()
    {
        Id = BuildingTypeId,
        Tier = 1,
        Chain = "economy",
        GoldCost = goldCost,
        WoodCost = woodCost,
        StoneCost = stoneCost,
        ManaCost = manaCost,
    };

    private static List<KingdomResource> CreateResources(Guid kingdomId, int gold = 200, int wood = 200, int stone = 200, int mana = 200) =>
    [
        new() { KingdomId = kingdomId, ResourceType = EResourceType.Gold, Amount = gold },
        new() { KingdomId = kingdomId, ResourceType = EResourceType.Food, Amount = 200 },
        new() { KingdomId = kingdomId, ResourceType = EResourceType.Wood, Amount = wood },
        new() { KingdomId = kingdomId, ResourceType = EResourceType.Stone, Amount = stone },
        new() { KingdomId = kingdomId, ResourceType = EResourceType.Mana, Amount = mana },
    ];

    [Fact]
    public void PlaceBuilding_ValidPlacement_ReturnsOkWithBuilding()
    {
        var game = CreateGameForPlacement(Kingdom1Id);
        var tile = CreateOwnedTile(Kingdom1Id);
        var bt = CreateBuildingType(goldCost: 100);
        var resources = CreateResources(Kingdom1Id, gold: 200);

        var result = game.PlaceBuilding(TileId, bt, tile, [], resources, 1.0m);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.TileId.ShouldBe(TileId);
        result.Value.BuildingTypeId.ShouldBe(BuildingTypeId);
        // Gold deducted: 200 - 100 = 100
        resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(100);
    }

    [Fact]
    public void PlaceBuilding_TileNotOwned_ReturnsFail()
    {
        var game = CreateGameForPlacement(Kingdom1Id);
        var tile = new Tile { Id = TileId, KingdomId = Guid.NewGuid() }; // different kingdom
        var bt = CreateBuildingType();
        var resources = CreateResources(Kingdom1Id);

        var result = game.PlaceBuilding(TileId, bt, tile, [], resources, 1.0m);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldContain("do not own");
    }

    [Fact]
    public void PlaceBuilding_TileAlreadyHasBuilding_ReturnsFail()
    {
        var game = CreateGameForPlacement(Kingdom1Id);
        var tile = CreateOwnedTile(Kingdom1Id);
        var bt = CreateBuildingType();
        var resources = CreateResources(Kingdom1Id);
        var existingBuildings = new List<Building>
        {
            new() { TileId = TileId, BuildingTypeId = Guid.NewGuid() },
        };

        var result = game.PlaceBuilding(TileId, bt, tile, existingBuildings, resources, 1.0m);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldContain("already has a building");
    }

    [Fact]
    public void PlaceBuilding_MissingPrerequisite_ReturnsFail()
    {
        var game = CreateGameForPlacement(Kingdom1Id);
        var tile = CreateOwnedTile(Kingdom1Id);
        var bt = CreateBuildingType();
        bt.PrerequisiteBuildingTypeId = PrereqBuildingTypeId;
        var resources = CreateResources(Kingdom1Id);

        var result = game.PlaceBuilding(TileId, bt, tile, [], resources, 1.0m);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldContain("prerequisite");
    }

    [Fact]
    public void PlaceBuilding_InsufficientGold_ReturnsFail()
    {
        var game = CreateGameForPlacement(Kingdom1Id);
        var tile = CreateOwnedTile(Kingdom1Id);
        var bt = CreateBuildingType(goldCost: 100);
        var resources = CreateResources(Kingdom1Id, gold: 50);

        var result = game.PlaceBuilding(TileId, bt, tile, [], resources, 1.0m);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldContain("Not enough Gold");
    }

    [Fact]
    public void PlaceBuilding_FactionCostModifier_Applied()
    {
        var game = CreateGameForPlacement(Kingdom1Id);
        var tile = CreateOwnedTile(Kingdom1Id);
        var bt = CreateBuildingType(goldCost: 100);
        var resources = CreateResources(Kingdom1Id, gold: 200);

        var result = game.PlaceBuilding(TileId, bt, tile, [], resources, 0.9m);

        result.IsSuccess.ShouldBeTrue();
        // floor(100 * 0.9) = 90, so 200 - 90 = 110
        resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(110);
    }

    [Fact]
    public void PlaceBuilding_AllOrNothing_NoPartialDeduction()
    {
        var game = CreateGameForPlacement(Kingdom1Id);
        var tile = CreateOwnedTile(Kingdom1Id);
        var bt = CreateBuildingType(goldCost: 100, woodCost: 100);
        var resources = CreateResources(Kingdom1Id, gold: 200, wood: 10); // gold OK, wood insufficient

        var result = game.PlaceBuilding(TileId, bt, tile, [], resources, 1.0m);

        result.IsSuccess.ShouldBeFalse();
        // No partial deduction: gold and wood unchanged
        resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(200);
        resources.Single(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(10);
    }
}
