using Base.Contracts;
using Domain.Buildings;
using Domain.Factions;
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

        var movingUnit = new MilitaryUnit
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

    // =========================================================================
    // Combat helpers
    // =========================================================================

    private static readonly Guid SwordsmanTypeId = Guid.Parse("cccccccc-0001-0000-0000-000000000001");
    private static readonly Guid ArcherTypeId = Guid.Parse("cccccccc-0001-0000-0000-000000000002");
    private static readonly Guid KnightTypeId = Guid.Parse("cccccccc-0001-0000-0000-000000000003");
    private static readonly Guid MageTypeId = Guid.Parse("cccccccc-0001-0000-0000-000000000004");

    private static UnitType CreateSwordsman() => new()
    {
        Id = SwordsmanTypeId,
        Name = new Base.LangStr("Swordsman", "en"),
        BaseStrength = 10,
    };

    private static UnitType CreateArcher() => new()
    {
        Id = ArcherTypeId,
        Name = new Base.LangStr("Archer", "en"),
        BaseStrength = 8,
    };

    private static UnitType CreateKnight() => new()
    {
        Id = KnightTypeId,
        Name = new Base.LangStr("Knight", "en"),
        BaseStrength = 15,
    };

    private static UnitType CreateMage() => new()
    {
        Id = MageTypeId,
        Name = new Base.LangStr("Mage", "en"),
        BaseStrength = 12,
    };

    private static List<UnitTypeMatchup> CreateMatchups()
    {
        // Subset of real matchup matrix relevant to tests
        return
        [
            new() { AttackerTypeId = SwordsmanTypeId, DefenderTypeId = SwordsmanTypeId, Multiplier = 1.00m },
            new() { AttackerTypeId = SwordsmanTypeId, DefenderTypeId = ArcherTypeId, Multiplier = 1.25m },
            new() { AttackerTypeId = SwordsmanTypeId, DefenderTypeId = KnightTypeId, Multiplier = 0.75m },
            new() { AttackerTypeId = SwordsmanTypeId, DefenderTypeId = MageTypeId, Multiplier = 0.75m },
            new() { AttackerTypeId = ArcherTypeId, DefenderTypeId = SwordsmanTypeId, Multiplier = 0.75m },
            new() { AttackerTypeId = ArcherTypeId, DefenderTypeId = ArcherTypeId, Multiplier = 1.00m },
            new() { AttackerTypeId = ArcherTypeId, DefenderTypeId = KnightTypeId, Multiplier = 1.25m },
            new() { AttackerTypeId = ArcherTypeId, DefenderTypeId = MageTypeId, Multiplier = 0.75m },
            new() { AttackerTypeId = KnightTypeId, DefenderTypeId = SwordsmanTypeId, Multiplier = 1.25m },
            new() { AttackerTypeId = KnightTypeId, DefenderTypeId = ArcherTypeId, Multiplier = 0.75m },
            new() { AttackerTypeId = KnightTypeId, DefenderTypeId = KnightTypeId, Multiplier = 1.00m },
            new() { AttackerTypeId = KnightTypeId, DefenderTypeId = MageTypeId, Multiplier = 1.25m },
            new() { AttackerTypeId = MageTypeId, DefenderTypeId = SwordsmanTypeId, Multiplier = 1.25m },
            new() { AttackerTypeId = MageTypeId, DefenderTypeId = ArcherTypeId, Multiplier = 0.75m },
            new() { AttackerTypeId = MageTypeId, DefenderTypeId = KnightTypeId, Multiplier = 0.75m },
            new() { AttackerTypeId = MageTypeId, DefenderTypeId = MageTypeId, Multiplier = 1.00m },
        ];
    }

    private static TerrainType CreateTerrain(decimal defenseBonus = 0.0m)
    {
        return new TerrainType
        {
            Id = Guid.NewGuid(),
            Name = new Base.LangStr("Plains", "en"),
            DefenseBonus = defenseBonus,
        };
    }

    private static Army CreateArmyWithUnits(Guid kingdomId, Guid tileId, List<MilitaryUnit> units)
    {
        var army = new Army
        {
            Id = Guid.NewGuid(),
            TileId = tileId,
            KingdomId = kingdomId,
            HasAttackedThisTurn = false,
            Units = units,
        };
        foreach (var u in units) u.ArmyId = army.Id;
        return army;
    }

    // =========================================================================
    // CalculateArmyStrength tests
    // =========================================================================

    [Fact]
    public void CalculateStrength_SingleUnitType_ReturnsBaseTimesMatchupTimesFaction()
    {
        // 10 Swordsmen vs 5 Archers, matchup=1.25, no faction bonus
        var swordsmanType = CreateSwordsman();
        var archerType = CreateArcher();
        var units = new List<MilitaryUnit>
        {
            new() { UnitTypeId = SwordsmanTypeId, Quantity = 10, UnitType = swordsmanType },
        };
        var opposing = new List<MilitaryUnit>
        {
            new() { UnitTypeId = ArcherTypeId, Quantity = 5, UnitType = archerType },
        };
        var matchups = CreateMatchups();

        var strength = Game.CalculateArmyStrength(units, opposing, matchups, new List<FactionUnitBonus>());

        // 10 * 10 * 1.25 * 1.0 = 125
        strength.ShouldBe(125m);
    }

    [Fact]
    public void CalculateStrength_MixedArmy_WeightsMatchupByOpposingProportion()
    {
        // 5 Swordsmen + 5 Archers vs 10 Knights
        var swordsmanType = CreateSwordsman(); // str=10
        var archerType = CreateArcher(); // str=8
        var knightType = CreateKnight(); // str=15
        var units = new List<MilitaryUnit>
        {
            new() { UnitTypeId = SwordsmanTypeId, Quantity = 5, UnitType = swordsmanType },
            new() { UnitTypeId = ArcherTypeId, Quantity = 5, UnitType = archerType },
        };
        var opposing = new List<MilitaryUnit>
        {
            new() { UnitTypeId = KnightTypeId, Quantity = 10, UnitType = knightType },
        };
        var matchups = CreateMatchups();

        var strength = Game.CalculateArmyStrength(units, opposing, matchups, new List<FactionUnitBonus>());

        // Swordsmen: 5 * 10 * matchup(Sword->Knight=0.75) * 1.0 = 37.5
        // Archers: 5 * 8 * matchup(Archer->Knight=1.25) * 1.0 = 50
        // Total = 87.5
        strength.ShouldBe(87.5m);
    }

    [Fact]
    public void CalculateStrength_FactionBonus_SpecificOverGlobal()
    {
        // Mage Council: specific Mage +20% (1.2), no global
        var mageType = CreateMage();
        var swordsmanType = CreateSwordsman();
        var units = new List<MilitaryUnit>
        {
            new() { UnitTypeId = MageTypeId, Quantity = 10, UnitType = mageType },
        };
        var opposing = new List<MilitaryUnit>
        {
            new() { UnitTypeId = SwordsmanTypeId, Quantity = 10, UnitType = swordsmanType },
        };
        var matchups = CreateMatchups();
        var factionBonuses = new List<FactionUnitBonus>
        {
            new() { FactionTypeId = Guid.NewGuid(), UnitTypeId = MageTypeId, Multiplier = 1.2m },
        };

        var strength = Game.CalculateArmyStrength(units, opposing, matchups, factionBonuses);

        // 10 * 12 * matchup(Mage->Swordsman=1.25) * 1.2 = 180
        strength.ShouldBe(180m);
    }

    [Fact]
    public void CalculateStrength_FactionBonus_GlobalFallback()
    {
        // Iron Throne: null UnitTypeId = 1.2 (applies to all)
        var swordsmanType = CreateSwordsman();
        var archerType = CreateArcher();
        var units = new List<MilitaryUnit>
        {
            new() { UnitTypeId = SwordsmanTypeId, Quantity = 10, UnitType = swordsmanType },
        };
        var opposing = new List<MilitaryUnit>
        {
            new() { UnitTypeId = ArcherTypeId, Quantity = 10, UnitType = archerType },
        };
        var matchups = CreateMatchups();
        var factionBonuses = new List<FactionUnitBonus>
        {
            new() { FactionTypeId = Guid.NewGuid(), UnitTypeId = null, Multiplier = 1.2m },
        };

        var strength = Game.CalculateArmyStrength(units, opposing, matchups, factionBonuses);

        // 10 * 10 * 1.25 * 1.2 = 150
        strength.ShouldBe(150m);
    }

    [Fact]
    public void CalculateStrength_NoOpposingUnits_ReturnsZero()
    {
        var swordsmanType = CreateSwordsman();
        var units = new List<MilitaryUnit>
        {
            new() { UnitTypeId = SwordsmanTypeId, Quantity = 10, UnitType = swordsmanType },
        };
        var opposing = new List<MilitaryUnit>();
        var matchups = CreateMatchups();

        var strength = Game.CalculateArmyStrength(units, opposing, matchups, new List<FactionUnitBonus>());

        strength.ShouldBe(0m);
    }

    // =========================================================================
    // ResolveCombat tests
    // =========================================================================

    [Fact]
    public void Attack_ValidAdjacentEnemy_ResolvesCombat()
    {
        // 10 Swordsmen attack 10 Archers on Plains
        // Attacker strength = 10*10*1.25*1.0 = 125
        // Defender strength = 10*8*0.75*1.0*(1+0.0) = 60
        // Attacker casualty ratio = min(1, 60/125) = 0.48
        // Defender casualty ratio = min(1, 125/60) = 1.0
        // Attacker losses = floor(10*0.48) = 4, remaining 6
        // Defender losses = floor(10*1.0) = 10, remaining 0 -> destroyed
        var game = CreateGame(Kingdom1Id);
        var swordsmanType = CreateSwordsman();
        var archerType = CreateArcher();
        var attackerTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var defenderTile = CreateTile(TileBId, Kingdom2Id, q: 1, r: 0);
        var terrain = CreateTerrain(0.0m);

        var attackerUnits = new List<MilitaryUnit>
        {
            new() { UnitTypeId = SwordsmanTypeId, Quantity = 10, UnitType = swordsmanType },
        };
        var defenderUnits = new List<MilitaryUnit>
        {
            new() { UnitTypeId = ArcherTypeId, Quantity = 10, UnitType = archerType },
        };
        var attackerArmy = CreateArmyWithUnits(Kingdom1Id, TileAId, attackerUnits);
        var defenderArmy = CreateArmyWithUnits(Kingdom2Id, TileBId, defenderUnits);
        var matchups = CreateMatchups();

        var result = game.ResolveCombat(attackerArmy, attackerTile, defenderTile, defenderArmy,
            matchups, new List<FactionUnitBonus>(), new List<FactionUnitBonus>(), terrain);

        result.IsSuccess.ShouldBeTrue();
        var combat = result.Value!;
        combat.AttackerStrength.ShouldBe(125m);
        combat.DefenderStrength.ShouldBe(60m);
        combat.WinnerKingdomId.ShouldBe(Kingdom1Id);
        combat.TileCaptured.ShouldBeTrue();
        combat.DefenderArmyDestroyed.ShouldBeTrue();
        combat.AttackerArmyDestroyed.ShouldBeFalse();
        // Attacker lost 4 swordsmen, remaining 6
        combat.AttackerCasualties.ShouldHaveSingleItem();
        combat.AttackerCasualties[0].Lost.ShouldBe(4);
        combat.AttackerCasualties[0].After.ShouldBe(6);
        // Tile captured
        defenderTile.KingdomId.ShouldBe(Kingdom1Id);
        attackerArmy.TileId.ShouldBe(TileBId);
    }

    [Fact]
    public void Attack_ArmyNotOwnedByKingdom_Fails()
    {
        var game = CreateGame(Kingdom1Id);
        var attackerTile = CreateTile(TileAId, Kingdom2Id, q: 0, r: 0);
        var defenderTile = CreateTile(TileBId, Kingdom2Id, q: 1, r: 0);
        var terrain = CreateTerrain();
        var attackerArmy = CreateArmyWithUnits(Kingdom2Id, TileAId, // wrong kingdom
            [new() { UnitTypeId = SwordsmanTypeId, Quantity = 5, UnitType = CreateSwordsman() }]);
        var defenderArmy = CreateArmyWithUnits(Kingdom2Id, TileBId,
            [new() { UnitTypeId = ArcherTypeId, Quantity = 5, UnitType = CreateArcher() }]);

        var result = game.ResolveCombat(attackerArmy, attackerTile, defenderTile, defenderArmy,
            CreateMatchups(), new List<FactionUnitBonus>(), new List<FactionUnitBonus>(), terrain);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("does not belong");
    }

    [Fact]
    public void Attack_NotAdjacent_Fails()
    {
        var game = CreateGame(Kingdom1Id);
        var attackerTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var defenderTile = CreateTile(TileBId, Kingdom2Id, q: 3, r: 3); // not adjacent
        var terrain = CreateTerrain();
        var attackerArmy = CreateArmyWithUnits(Kingdom1Id, TileAId,
            [new() { UnitTypeId = SwordsmanTypeId, Quantity = 5, UnitType = CreateSwordsman() }]);
        var defenderArmy = CreateArmyWithUnits(Kingdom2Id, TileBId,
            [new() { UnitTypeId = ArcherTypeId, Quantity = 5, UnitType = CreateArcher() }]);

        var result = game.ResolveCombat(attackerArmy, attackerTile, defenderTile, defenderArmy,
            CreateMatchups(), new List<FactionUnitBonus>(), new List<FactionUnitBonus>(), terrain);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("not adjacent");
    }

    [Fact]
    public void Attack_NoEnemyOnTile_Fails()
    {
        var game = CreateGame(Kingdom1Id);
        var attackerTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var defenderTile = CreateTile(TileBId, Kingdom2Id, q: 1, r: 0);
        var terrain = CreateTerrain();
        var attackerArmy = CreateArmyWithUnits(Kingdom1Id, TileAId,
            [new() { UnitTypeId = SwordsmanTypeId, Quantity = 5, UnitType = CreateSwordsman() }]);
        // Defender army has no units with quantity > 0
        var defenderArmy = CreateArmyWithUnits(Kingdom2Id, TileBId,
            [new() { UnitTypeId = ArcherTypeId, Quantity = 0, UnitType = CreateArcher() }]);

        var result = game.ResolveCombat(attackerArmy, attackerTile, defenderTile, defenderArmy,
            CreateMatchups(), new List<FactionUnitBonus>(), new List<FactionUnitBonus>(), terrain);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("No enemy");
    }

    [Fact]
    public void Attack_AlreadyAttacked_Fails()
    {
        var game = CreateGame(Kingdom1Id);
        var attackerTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var defenderTile = CreateTile(TileBId, Kingdom2Id, q: 1, r: 0);
        var terrain = CreateTerrain();
        var attackerArmy = CreateArmyWithUnits(Kingdom1Id, TileAId,
            [new() { UnitTypeId = SwordsmanTypeId, Quantity = 5, UnitType = CreateSwordsman() }]);
        attackerArmy.HasAttackedThisTurn = true; // already attacked
        var defenderArmy = CreateArmyWithUnits(Kingdom2Id, TileBId,
            [new() { UnitTypeId = ArcherTypeId, Quantity = 5, UnitType = CreateArcher() }]);

        var result = game.ResolveCombat(attackerArmy, attackerTile, defenderTile, defenderArmy,
            CreateMatchups(), new List<FactionUnitBonus>(), new List<FactionUnitBonus>(), terrain);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("already attacked");
    }

    [Fact]
    public void Attack_DefenderWins_AttackerDestroyed()
    {
        // Weak attacker: 2 Archers (str=8) vs 10 Knights (str=15)
        // Attacker: 2*8*matchup(Archer->Knight=1.25)*1.0 = 20
        // Defender: 10*15*matchup(Knight->Archer=0.75)*1.0 = 112.5
        // Attacker casualty = min(1, 112.5/20) = 1.0 -> all archers die
        // Defender casualty = min(1, 20/112.5) = 0.177.. -> floor(10*0.177) = 1 knight dies
        var game = CreateGame(Kingdom1Id);
        var archerType = CreateArcher();
        var knightType = CreateKnight();
        var attackerTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var defenderTile = CreateTile(TileBId, Kingdom2Id, q: 1, r: 0);
        var terrain = CreateTerrain(0.0m);

        var attackerArmy = CreateArmyWithUnits(Kingdom1Id, TileAId,
            [new() { UnitTypeId = ArcherTypeId, Quantity = 2, UnitType = archerType }]);
        var defenderArmy = CreateArmyWithUnits(Kingdom2Id, TileBId,
            [new() { UnitTypeId = KnightTypeId, Quantity = 10, UnitType = knightType }]);

        var result = game.ResolveCombat(attackerArmy, attackerTile, defenderTile, defenderArmy,
            CreateMatchups(), new List<FactionUnitBonus>(), new List<FactionUnitBonus>(), terrain);

        result.IsSuccess.ShouldBeTrue();
        var combat = result.Value!;
        combat.WinnerKingdomId.ShouldBe(Kingdom2Id);
        combat.AttackerArmyDestroyed.ShouldBeTrue();
        combat.DefenderArmyDestroyed.ShouldBeFalse();
        combat.TileCaptured.ShouldBeFalse();
        defenderTile.KingdomId.ShouldBe(Kingdom2Id); // unchanged
    }

    [Fact]
    public void Attack_MutualDestruction_Draw()
    {
        // Equal armies that wipe each other out
        // 1 Swordsman vs 1 Swordsman on Plains
        // Both strength = 1*10*1.0*1.0 = 10
        // Both casualty ratio = 1.0 -> both lose floor(1*1.0) = 1 -> both destroyed
        var game = CreateGame(Kingdom1Id);
        var swordsmanType = CreateSwordsman();
        var attackerTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var defenderTile = CreateTile(TileBId, Kingdom2Id, q: 1, r: 0);
        var terrain = CreateTerrain(0.0m);

        var attackerArmy = CreateArmyWithUnits(Kingdom1Id, TileAId,
            [new() { UnitTypeId = SwordsmanTypeId, Quantity = 1, UnitType = swordsmanType }]);
        var defenderArmy = CreateArmyWithUnits(Kingdom2Id, TileBId,
            [new() { UnitTypeId = SwordsmanTypeId, Quantity = 1, UnitType = swordsmanType }]);

        var result = game.ResolveCombat(attackerArmy, attackerTile, defenderTile, defenderArmy,
            CreateMatchups(), new List<FactionUnitBonus>(), new List<FactionUnitBonus>(), terrain);

        result.IsSuccess.ShouldBeTrue();
        var combat = result.Value!;
        combat.WinnerKingdomId.ShouldBeNull(); // draw
        combat.AttackerArmyDestroyed.ShouldBeTrue();
        combat.DefenderArmyDestroyed.ShouldBeTrue();
        combat.TileCaptured.ShouldBeFalse();
        defenderTile.KingdomId.ShouldBe(Kingdom2Id); // unchanged
    }

    [Fact]
    public void Attack_AttackerWins_CapturesTile()
    {
        // 10 Knights (str=15) vs 3 Archers (str=8) on Plains
        // Attacker: 10*15*0.75*1.0 = 112.5
        // Defender: 3*8*1.25*1.0 = 30
        // Attacker casualty = min(1, 30/112.5) = 0.2666 -> floor(10*0.266) = 2 lost
        // Defender casualty = min(1, 112.5/30) = 1.0 -> all die
        var game = CreateGame(Kingdom1Id);
        var knightType = CreateKnight();
        var archerType = CreateArcher();
        var attackerTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var defenderTile = CreateTile(TileBId, Kingdom2Id, q: 1, r: 0);
        var terrain = CreateTerrain(0.0m);

        var attackerArmy = CreateArmyWithUnits(Kingdom1Id, TileAId,
            [new() { UnitTypeId = KnightTypeId, Quantity = 10, UnitType = knightType }]);
        var defenderArmy = CreateArmyWithUnits(Kingdom2Id, TileBId,
            [new() { UnitTypeId = ArcherTypeId, Quantity = 3, UnitType = archerType }]);

        var result = game.ResolveCombat(attackerArmy, attackerTile, defenderTile, defenderArmy,
            CreateMatchups(), new List<FactionUnitBonus>(), new List<FactionUnitBonus>(), terrain);

        result.IsSuccess.ShouldBeTrue();
        var combat = result.Value!;
        combat.WinnerKingdomId.ShouldBe(Kingdom1Id);
        combat.TileCaptured.ShouldBeTrue();
        combat.DefenderArmyDestroyed.ShouldBeTrue();
        defenderTile.KingdomId.ShouldBe(Kingdom1Id);
        attackerArmy.TileId.ShouldBe(TileBId);
    }

    [Fact]
    public void Attack_DefenderTerrainBonus_Applied()
    {
        // 10 Swordsmen vs 10 Swordsmen on Forest (0.20 defense)
        // Attacker strength = 10*10*1.0*1.0 = 100
        // Defender strength = 10*10*1.0*1.0 * (1+0.20) = 120
        // Attacker casualty = min(1, 120/100) = 1.0
        // Defender casualty = min(1, 100/120) = 0.8333 -> floor(10*0.833) = 8 lost, 2 remain
        var game = CreateGame(Kingdom1Id);
        var swordsmanType = CreateSwordsman();
        var attackerTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var defenderTile = CreateTile(TileBId, Kingdom2Id, q: 1, r: 0);
        var terrain = CreateTerrain(0.20m); // Forest

        var attackerArmy = CreateArmyWithUnits(Kingdom1Id, TileAId,
            [new() { UnitTypeId = SwordsmanTypeId, Quantity = 10, UnitType = swordsmanType }]);
        var defenderArmy = CreateArmyWithUnits(Kingdom2Id, TileBId,
            [new() { UnitTypeId = SwordsmanTypeId, Quantity = 10, UnitType = swordsmanType }]);

        var result = game.ResolveCombat(attackerArmy, attackerTile, defenderTile, defenderArmy,
            CreateMatchups(), new List<FactionUnitBonus>(), new List<FactionUnitBonus>(), terrain);

        result.IsSuccess.ShouldBeTrue();
        var combat = result.Value!;
        combat.AttackerStrength.ShouldBe(100m);
        combat.DefenderStrength.ShouldBe(120m);
        // Attacker loses all (ratio = 1.0)
        combat.AttackerArmyDestroyed.ShouldBeTrue();
        // Defender loses 8, keeps 2
        combat.DefenderCasualties[0].Lost.ShouldBe(8);
        combat.DefenderCasualties[0].After.ShouldBe(2);
        combat.WinnerKingdomId.ShouldBe(Kingdom2Id);
    }

    [Fact]
    public void Attack_ProportionalCasualties_Floored()
    {
        // Mixed army: 7 Swordsmen + 3 Archers attack 5 Knights on Plains
        // Attacker:
        //   Swordsmen: 7*10*matchup(Sword->Knight=0.75)*1.0 = 52.5
        //   Archers: 3*8*matchup(Archer->Knight=1.25)*1.0 = 30
        //   Total = 82.5
        // Defender:
        //   Knights vs mixed: weighted matchup = (1.25*7 + 0.75*3)/10 = (8.75+2.25)/10 = 1.1
        //   5*15*1.1*1.0 = 82.5
        //   With Plains (0.0): 82.5 * 1.0 = 82.5
        // Equal strength -> both casualty ratios = 1.0
        // All units die -> mutual destruction
        var game = CreateGame(Kingdom1Id);
        var swordsmanType = CreateSwordsman();
        var archerType = CreateArcher();
        var knightType = CreateKnight();
        var attackerTile = CreateTile(TileAId, Kingdom1Id, q: 0, r: 0);
        var defenderTile = CreateTile(TileBId, Kingdom2Id, q: 1, r: 0);
        var terrain = CreateTerrain(0.0m);

        var attackerArmy = CreateArmyWithUnits(Kingdom1Id, TileAId,
        [
            new() { UnitTypeId = SwordsmanTypeId, Quantity = 7, UnitType = swordsmanType },
            new() { UnitTypeId = ArcherTypeId, Quantity = 3, UnitType = archerType },
        ]);
        var defenderArmy = CreateArmyWithUnits(Kingdom2Id, TileBId,
            [new() { UnitTypeId = KnightTypeId, Quantity = 5, UnitType = knightType }]);

        var result = game.ResolveCombat(attackerArmy, attackerTile, defenderTile, defenderArmy,
            CreateMatchups(), new List<FactionUnitBonus>(), new List<FactionUnitBonus>(), terrain);

        result.IsSuccess.ShouldBeTrue();
        var combat = result.Value!;
        // Both at 82.5 => both casualty ratio = 1.0, all die
        combat.AttackerArmyDestroyed.ShouldBeTrue();
        combat.DefenderArmyDestroyed.ShouldBeTrue();
        combat.WinnerKingdomId.ShouldBeNull(); // draw
        // Verify floor rounding on casualties
        var swordsmanCasualty = combat.AttackerCasualties.First(c => c.UnitTypeId == SwordsmanTypeId);
        swordsmanCasualty.Before.ShouldBe(7);
        swordsmanCasualty.Lost.ShouldBe(7); // floor(7*1.0) = 7
        var archerCasualty = combat.AttackerCasualties.First(c => c.UnitTypeId == ArcherTypeId);
        archerCasualty.Before.ShouldBe(3);
        archerCasualty.Lost.ShouldBe(3); // floor(3*1.0) = 3
    }
}
