using Base.Contracts;
using Domain.Buildings;
using Domain.Game;
using Domain.Map;
using Domain.Military;
using Domain.Resources;
using Shouldly;
using MilitaryUnit = Domain.Military.Unit;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// Pure domain-level unit tests for Game.TrainTroops() and Game.MoveArmy().
/// No mocks needed -- tests create entities directly and call domain methods.
/// </summary>
public class GameDomainMilitaryTests
{
    private static readonly Guid Kingdom1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Kingdom2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TileAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TileBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid BuildingId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid BuildingTypeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid UnitTypeId1 = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid UnitTypeId2 = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Game CreateGame(Guid currentKingdomId)
    {
        return new Game
        {
            Id = Guid.NewGuid(),
            Status = EGameStatus.InProgress,
            TurnNumber = 1,
            CurrentTurnKingdomId = currentKingdomId,
            Kingdoms = new List<Kingdom>
            {
                new() { Id = Kingdom1Id, CreatedAt = DateTime.UtcNow },
                new() { Id = Kingdom2Id, CreatedAt = DateTime.UtcNow },
            },
        };
    }

    private static UnitType CreateUnitType(Guid? id = null, int goldCost = 10, int foodCost = 5,
        int woodCost = 0, int stoneCost = 0, int manaCost = 0)
    {
        return new UnitType
        {
            Id = id ?? UnitTypeId1,
            Name = new Base.LangStr("Swordsman", "en"),
            BaseStrength = 10,
            GoldCost = goldCost,
            FoodCost = foodCost,
            WoodCost = woodCost,
            StoneCost = stoneCost,
            ManaCost = manaCost,
        };
    }

    private static Building CreateBuilding(bool hasTrained = false)
    {
        return new Building
        {
            Id = BuildingId,
            TileId = TileAId,
            BuildingTypeId = BuildingTypeId,
            HasTrainedThisTurn = hasTrained,
        };
    }

    private static BuildingUnitType CreateBuildingUnitType()
    {
        return new BuildingUnitType
        {
            BuildingTypeId = BuildingTypeId,
            UnitTypeId = UnitTypeId1,
        };
    }

    private static Tile CreateTile(Guid tileId, Guid? kingdomId, int q = 0, int r = 0)
    {
        return new Tile
        {
            Id = tileId,
            KingdomId = kingdomId,
            CoordQ = q,
            CoordR = r,
            GameId = Guid.NewGuid(),
        };
    }

    private static List<KingdomResource> CreateResources(Guid kingdomId,
        int gold = 200, int food = 200, int wood = 200, int stone = 200, int mana = 200)
    {
        return
        [
            new() { KingdomId = kingdomId, ResourceType = EResourceType.Gold, Amount = gold },
            new() { KingdomId = kingdomId, ResourceType = EResourceType.Food, Amount = food },
            new() { KingdomId = kingdomId, ResourceType = EResourceType.Wood, Amount = wood },
            new() { KingdomId = kingdomId, ResourceType = EResourceType.Stone, Amount = stone },
            new() { KingdomId = kingdomId, ResourceType = EResourceType.Mana, Amount = mana },
        ];
    }

    private static Army CreateArmy(Guid kingdomId, Guid tileId, List<MilitaryUnit>? units = null)
    {
        var army = new Army
        {
            Id = Guid.NewGuid(),
            TileId = tileId,
            KingdomId = kingdomId,
            Units = units ?? new List<MilitaryUnit>(),
        };
        return army;
    }

    // =========================================================================
    // TrainTroops tests
    // =========================================================================

    [Fact]
    public void Train_ValidRequest_CreatesArmyAndUnits()
    {
        var game = CreateGame(Kingdom1Id);
        var tile = CreateTile(TileAId, Kingdom1Id);
        var building = CreateBuilding();
        var unitType = CreateUnitType(goldCost: 10, foodCost: 5);
        var but = CreateBuildingUnitType();
        var resources = CreateResources(Kingdom1Id, gold: 200, food: 200);

        var result = game.TrainTroops(building, but, unitType, 3, null, tile, resources, 1.0m);

        result.IsSuccess.ShouldBeTrue();
        var (army, units) = result.Value!;
        army.KingdomId.ShouldBe(Kingdom1Id);
        army.TileId.ShouldBe(TileAId);
        units.Count.ShouldBe(1);
        units[0].UnitTypeId.ShouldBe(UnitTypeId1);
        units[0].Quantity.ShouldBe(3);
        building.HasTrainedThisTurn.ShouldBeTrue();
        // Resources deducted: gold = 200 - 30, food = 200 - 15
        resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(170);
        resources.Single(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(185);
    }

    [Fact]
    public void Train_BuildingNotOwnedByKingdom_Fails()
    {
        var game = CreateGame(Kingdom1Id);
        var tile = CreateTile(TileAId, Kingdom2Id); // different kingdom owns tile
        var building = CreateBuilding();
        var unitType = CreateUnitType();
        var but = CreateBuildingUnitType();
        var resources = CreateResources(Kingdom1Id);

        var result = game.TrainTroops(building, but, unitType, 1, null, tile, resources, 1.0m);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("do not own");
    }

    [Fact]
    public void Train_BuildingCannotProduceUnit_Fails()
    {
        var game = CreateGame(Kingdom1Id);
        var tile = CreateTile(TileAId, Kingdom1Id);
        var building = CreateBuilding();
        var unitType = CreateUnitType();
        var resources = CreateResources(Kingdom1Id);

        var result = game.TrainTroops(building, null, unitType, 1, null, tile, resources, 1.0m);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("cannot produce");
    }

    [Fact]
    public void Train_BuildingAlreadyTrained_Fails()
    {
        var game = CreateGame(Kingdom1Id);
        var tile = CreateTile(TileAId, Kingdom1Id);
        var building = CreateBuilding(hasTrained: true);
        var unitType = CreateUnitType();
        var but = CreateBuildingUnitType();
        var resources = CreateResources(Kingdom1Id);

        var result = game.TrainTroops(building, but, unitType, 1, null, tile, resources, 1.0m);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("already trained");
    }

    [Fact]
    public void Train_CannotAfford_Fails()
    {
        var game = CreateGame(Kingdom1Id);
        var tile = CreateTile(TileAId, Kingdom1Id);
        var building = CreateBuilding();
        var unitType = CreateUnitType(goldCost: 100); // 100 * 3 = 300 gold needed
        var but = CreateBuildingUnitType();
        var resources = CreateResources(Kingdom1Id, gold: 50); // only 50

        var result = game.TrainTroops(building, but, unitType, 3, null, tile, resources, 1.0m);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Not enough");
        // No partial deduction
        resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(50);
    }

    [Fact]
    public void Train_ExistingArmy_MergesUnits()
    {
        var game = CreateGame(Kingdom1Id);
        var tile = CreateTile(TileAId, Kingdom1Id);
        var building = CreateBuilding();
        var unitType = CreateUnitType();
        var but = CreateBuildingUnitType();
        var resources = CreateResources(Kingdom1Id);

        var existingUnit = new MilitaryUnit { ArmyId = Guid.NewGuid(), UnitTypeId = UnitTypeId1, Quantity = 5 };
        var existingArmy = CreateArmy(Kingdom1Id, TileAId, [existingUnit]);

        var result = game.TrainTroops(building, but, unitType, 3, existingArmy, tile, resources, 1.0m);

        result.IsSuccess.ShouldBeTrue();
        var (army, units) = result.Value!;
        army.ShouldBe(existingArmy);
        // Existing unit quantity should be merged: 5 + 3 = 8
        existingUnit.Quantity.ShouldBe(8);
    }

    [Fact]
    public void Train_ExistingArmy_NewUnitType_AddsEntry()
    {
        var game = CreateGame(Kingdom1Id);
        var tile = CreateTile(TileAId, Kingdom1Id);
        var building = CreateBuilding();
        var unitType = CreateUnitType(id: UnitTypeId2); // different unit type
        var but = new BuildingUnitType { BuildingTypeId = BuildingTypeId, UnitTypeId = UnitTypeId2 };
        var resources = CreateResources(Kingdom1Id);

        var existingUnit = new MilitaryUnit { ArmyId = Guid.NewGuid(), UnitTypeId = UnitTypeId1, Quantity = 5 };
        var existingArmy = CreateArmy(Kingdom1Id, TileAId, [existingUnit]);

        var result = game.TrainTroops(building, but, unitType, 2, existingArmy, tile, resources, 1.0m);

        result.IsSuccess.ShouldBeTrue();
        var (army, units) = result.Value!;
        army.Units!.Count.ShouldBe(2); // original + new
        existingUnit.Quantity.ShouldBe(5); // untouched
    }

    [Fact]
    public void Train_FactionCostModifier_Applied()
    {
        var game = CreateGame(Kingdom1Id);
        var tile = CreateTile(TileAId, Kingdom1Id);
        var building = CreateBuilding();
        var unitType = CreateUnitType(goldCost: 10, foodCost: 10);
        var but = CreateBuildingUnitType();
        var resources = CreateResources(Kingdom1Id, gold: 200, food: 200);

        // cost = unitCost * quantity * factionModifier
        // gold: floor(10 * 5 * 0.9) = floor(45) = 45
        // food: floor(10 * 5 * 0.9) = floor(45) = 45
        var result = game.TrainTroops(building, but, unitType, 5, null, tile, resources, 0.9m);

        result.IsSuccess.ShouldBeTrue();
        resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(155);
        resources.Single(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(155);
    }

    // =========================================================================
    // MoveArmy tests
    // =========================================================================

    [Fact]
    public void Move_ValidAdjacentTile_MovesArmy()
    {
        var game = CreateGame(Kingdom1Id);
        var sourceTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var targetTile = CreateTile(TileBId, Kingdom1Id, q: 1, r: 0); // adjacent
        var army = CreateArmy(Kingdom1Id, TileAId);

        var result = game.MoveArmy(army, sourceTile, targetTile, null, null);

        result.IsSuccess.ShouldBeTrue();
        var (tileClaimed, armyMerged, mergedInto) = result.Value!;
        army.TileId.ShouldBe(TileBId);
        tileClaimed.ShouldBeFalse();
        armyMerged.ShouldBeFalse();
        mergedInto.ShouldBeNull();
    }

    [Fact]
    public void Move_NotAdjacent_Fails()
    {
        var game = CreateGame(Kingdom1Id);
        var sourceTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var targetTile = CreateTile(TileBId, Kingdom1Id, q: 3, r: 3); // not adjacent
        var army = CreateArmy(Kingdom1Id, TileAId);

        var result = game.MoveArmy(army, sourceTile, targetTile, null, null);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("not adjacent");
    }

    [Fact]
    public void Move_EnemyOccupied_Fails()
    {
        var game = CreateGame(Kingdom1Id);
        var sourceTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var targetTile = CreateTile(TileBId, Kingdom2Id, q: 1, r: 0);
        var army = CreateArmy(Kingdom1Id, TileAId);
        var enemyArmy = CreateArmy(Kingdom2Id, TileBId);

        var result = game.MoveArmy(army, sourceTile, targetTile, null, enemyArmy);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("enemy");
    }

    [Fact]
    public void Move_UnownedTile_ClaimsIt()
    {
        var game = CreateGame(Kingdom1Id);
        var sourceTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var targetTile = CreateTile(TileBId, null, q: 1, r: 0); // unowned
        var army = CreateArmy(Kingdom1Id, TileAId);

        var result = game.MoveArmy(army, sourceTile, targetTile, null, null);

        result.IsSuccess.ShouldBeTrue();
        var (tileClaimed, armyMerged, _) = result.Value!;
        tileClaimed.ShouldBeTrue();
        targetTile.KingdomId.ShouldBe(Kingdom1Id);
    }

    [Fact]
    public void Move_FriendlyArmyOnTile_Merges()
    {
        var game = CreateGame(Kingdom1Id);
        var sourceTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var targetTile = CreateTile(TileBId, Kingdom1Id, q: 1, r: 0);

        var movingUnit = new MilitaryUnit { UnitTypeId = UnitTypeId1, Quantity = 3, UnitType = CreateUnitType() };
        var movingArmy = CreateArmy(Kingdom1Id, TileAId, [movingUnit]);

        var destUnit = new MilitaryUnit { UnitTypeId = UnitTypeId1, Quantity = 5 };
        var destArmy = CreateArmy(Kingdom1Id, TileBId, [destUnit]);

        var result = game.MoveArmy(movingArmy, sourceTile, targetTile, destArmy, null);

        result.IsSuccess.ShouldBeTrue();
        var (tileClaimed, armyMerged, mergedInto) = result.Value!;
        armyMerged.ShouldBeTrue();
        mergedInto.ShouldBe(destArmy);
        // Same unit type: quantities merged
        destUnit.Quantity.ShouldBe(8); // 5 + 3
    }

    [Fact]
    public void Move_FriendlyArmy_DifferentUnitTypes_Merge()
    {
        var game = CreateGame(Kingdom1Id);
        var sourceTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var targetTile = CreateTile(TileBId, Kingdom1Id, q: 1, r: 0);

        var movingUnit = new Unit
        {
            UnitTypeId = UnitTypeId1, Quantity = 3,
            UnitType = CreateUnitType(id: UnitTypeId1),
        };
        var movingArmy = CreateArmy(Kingdom1Id, TileAId, [movingUnit]);

        var destUnit = new MilitaryUnit { UnitTypeId = UnitTypeId2, Quantity = 5 };
        var destArmy = CreateArmy(Kingdom1Id, TileBId, [destUnit]);

        var result = game.MoveArmy(movingArmy, sourceTile, targetTile, destArmy, null);

        result.IsSuccess.ShouldBeTrue();
        var (_, armyMerged, mergedInto) = result.Value!;
        armyMerged.ShouldBeTrue();
        mergedInto!.Units!.Count.ShouldBe(2); // Archer + Swordsman
        destUnit.Quantity.ShouldBe(5); // untouched
    }
}
