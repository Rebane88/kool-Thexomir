using Application.Contracts;
using Application.Services.Military;
using Application.Services.Military.DTOs;
using Base;
using Base.Contracts;
using Domain.Buildings;
using Domain.Factions;
using Domain.Game;
using Domain.Map;
using Domain.Military;
using Domain.Resources;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using MilitaryUnit = Domain.Military.Unit;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// Service-level unit tests for MilitaryService.
/// Fully mocked IUnitOfWork and IGameGuard, following BuildingServiceTests pattern.
/// </summary>
public class MilitaryServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGameGuard> _gameGuardMock = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<IGameRepository> _gamesMock = new();
    private readonly Mock<IBuildingRepository> _buildingsMock = new();
    private readonly Mock<IBuildingTypeRepository> _buildingTypesMock = new();
    private readonly Mock<IBuildingUnitTypeRepository> _buildingUnitTypesMock = new();
    private readonly Mock<ITileRepository> _tilesMock = new();
    private readonly Mock<IArmyRepository> _armiesMock = new();
    private readonly Mock<IUnitRepository> _unitsMock = new();
    private readonly Mock<IUnitTypeRepository> _unitTypesMock = new();
    private readonly Mock<IKingdomResourceRepository> _kingdomResourcesMock = new();
    private readonly Mock<IFactionTypeRepository> _factionTypesMock = new();
    private readonly Mock<ITurnLogRepository> _turnLogsMock = new();
    private readonly Mock<IKingdomRepository> _kingdomsMock = new();
    private readonly Mock<IUnitTypeMatchupRepository> _unitTypeMatchupsMock = new();
    private readonly Mock<IBattleRepository> _battlesMock = new();
    private readonly Mock<IFactionUnitBonusRepository> _factionUnitBonusesMock = new();
    private readonly Mock<ITerrainTypeRepository> _terrainTypesMock = new();
    private readonly MilitaryService _sut;

    // Captured entities
    private readonly List<Army> _addedArmies = [];
    private readonly List<MilitaryUnit> _addedUnits = [];
    private readonly List<TurnLog> _addedTurnLogs = [];
    private readonly List<Battle> _addedBattles = [];

    // Well-known IDs
    private static readonly Guid GameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid KingdomId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserId = Guid.Parse("eeeeeeee-1111-1111-1111-111111111111");
    private static readonly Guid TileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid TargetTileId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccdd");
    private static readonly Guid BuildingId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid BuildingTypeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid UnitTypeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid ArmyId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid FactionId = Guid.Parse("eeeeeeee-0001-0000-0000-000000000001");

    public MilitaryServiceTests()
    {
        var services = new ServiceCollection();
        services.AddKeyedScoped<IWinConditionChecker, EliminationChecker>(EWinCondition.Elimination);
        services.AddKeyedScoped<IWinConditionChecker, ScoreChecker>(EWinCondition.Score);
        _serviceProvider = services.BuildServiceProvider();

        _gamesMock.Setup(g => g.UpdateAsync(It.IsAny<Game>())).ReturnsAsync((Game g) => g);
        _unitOfWorkMock.Setup(u => u.Games).Returns(_gamesMock.Object);
        _unitOfWorkMock.Setup(u => u.Buildings).Returns(_buildingsMock.Object);
        _unitOfWorkMock.Setup(u => u.BuildingTypes).Returns(_buildingTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.BuildingUnitTypes).Returns(_buildingUnitTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.Tiles).Returns(_tilesMock.Object);
        _unitOfWorkMock.Setup(u => u.Armies).Returns(_armiesMock.Object);
        _unitOfWorkMock.Setup(u => u.Units).Returns(_unitsMock.Object);
        _unitOfWorkMock.Setup(u => u.UnitTypes).Returns(_unitTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.KingdomResources).Returns(_kingdomResourcesMock.Object);
        _unitOfWorkMock.Setup(u => u.FactionTypes).Returns(_factionTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.TurnLogs).Returns(_turnLogsMock.Object);
        _unitOfWorkMock.Setup(u => u.Kingdoms).Returns(_kingdomsMock.Object);
        _unitOfWorkMock.Setup(u => u.UnitTypeMatchups).Returns(_unitTypeMatchupsMock.Object);
        _unitOfWorkMock.Setup(u => u.Battles).Returns(_battlesMock.Object);
        _unitOfWorkMock.Setup(u => u.FactionUnitBonuses).Returns(_factionUnitBonusesMock.Object);
        _unitOfWorkMock.Setup(u => u.TerrainTypes).Returns(_terrainTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        _armiesMock.Setup(a => a.AddAsync(It.IsAny<Army>()))
            .ReturnsAsync((Army a) => { _addedArmies.Add(a); return a; });
        _armiesMock.Setup(a => a.UpdateAsync(It.IsAny<Army>()))
            .ReturnsAsync((Army a) => a);
        _unitsMock.Setup(u => u.AddAsync(It.IsAny<MilitaryUnit>()))
            .ReturnsAsync((MilitaryUnit u) => { _addedUnits.Add(u); return u; });
        _unitsMock.Setup(u => u.UpdateAsync(It.IsAny<MilitaryUnit>()))
            .ReturnsAsync((MilitaryUnit u) => u);
        _turnLogsMock.Setup(t => t.AddAsync(It.IsAny<TurnLog>()))
            .ReturnsAsync((TurnLog t) => { _addedTurnLogs.Add(t); return t; });
        _buildingsMock.Setup(b => b.UpdateAsync(It.IsAny<Building>()))
            .ReturnsAsync((Building b) => b);
        _kingdomResourcesMock.Setup(r => r.UpdateAsync(It.IsAny<KingdomResource>()))
            .ReturnsAsync((KingdomResource r) => r);
        _tilesMock.Setup(t => t.UpdateAsync(It.IsAny<Tile>()))
            .ReturnsAsync((Tile t) => t);
        _battlesMock.Setup(b => b.AddAsync(It.IsAny<Battle>()))
            .ReturnsAsync((Battle b) => { _addedBattles.Add(b); return b; });

        _sut = new MilitaryService(_unitOfWorkMock.Object, _gameGuardMock.Object, _serviceProvider);
    }

    private void SetupGuardSuccess()
    {
        var game = new Game
        {
            Id = GameId, Status = EGameStatus.InProgress, TurnNumber = 1,
            CurrentTurnKingdomId = KingdomId
        };
        var kingdom = new Kingdom
        {
            Id = KingdomId, GameId = GameId, AppUserId = UserId, FactionTypeId = FactionId
        };
        _gameGuardMock.Setup(g => g.ValidateAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Ok(new GameGuardContext(game, kingdom)));
    }

    private void SetupGuardFailure(string error = "Game not found.")
    {
        _gameGuardMock.Setup(g => g.ValidateAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Fail(error));
    }

    private static TrainTroopsRequest DefaultTrainRequest() => new()
    {
        BuildingId = BuildingId,
        UnitTypeId = UnitTypeId,
        Quantity = 3,
    };

    private static MoveArmyRequest DefaultMoveRequest() => new()
    {
        ArmyId = ArmyId,
        TargetTileId = TargetTileId,
    };

    private void SetupTrainHappyPath()
    {
        SetupGuardSuccess();

        var building = new Building { Id = BuildingId, TileId = TileId, BuildingTypeId = BuildingTypeId };
        _buildingsMock.Setup(b => b.GetByIdAsync(BuildingId)).ReturnsAsync(building);

        var tile = new Tile { Id = TileId, KingdomId = KingdomId, GameId = GameId };
        _tilesMock.Setup(t => t.GetByIdAsync(TileId)).ReturnsAsync(tile);

        var but = new BuildingUnitType { BuildingTypeId = BuildingTypeId, UnitTypeId = UnitTypeId };
        _buildingUnitTypesMock.Setup(b => b.GetByBuildingAndUnitTypeAsync(BuildingTypeId, UnitTypeId))
            .ReturnsAsync(but);

        var unitType = new UnitType
        {
            Id = UnitTypeId, Name = new LangStr("Swordsman", "en"),
            BaseStrength = 10, GoldCost = 10, FoodCost = 5
        };
        _unitTypesMock.Setup(u => u.GetByIdAsync(UnitTypeId)).ReturnsAsync(unitType);

        var resources = new List<KingdomResource>
        {
            new() { KingdomId = KingdomId, ResourceType = EResourceType.Gold, Amount = 200 },
            new() { KingdomId = KingdomId, ResourceType = EResourceType.Food, Amount = 200 },
            new() { KingdomId = KingdomId, ResourceType = EResourceType.Wood, Amount = 200 },
            new() { KingdomId = KingdomId, ResourceType = EResourceType.Stone, Amount = 200 },
            new() { KingdomId = KingdomId, ResourceType = EResourceType.Mana, Amount = 200 },
        };
        _kingdomResourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(KingdomId)).ReturnsAsync(resources);

        var faction = new FactionType { Id = FactionId, Name = new LangStr("Test", "en"), BuildingCostModifier = 1.0m };
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId)).ReturnsAsync(faction);

        _armiesMock.Setup(a => a.GetArmyOnTileForKingdomAsync(TileId, KingdomId)).ReturnsAsync((Army?)null);
    }

    private void SetupMoveHappyPath()
    {
        SetupGuardSuccess();

        var army = new Army
        {
            Id = ArmyId, TileId = TileId, KingdomId = KingdomId,
            Units = new List<MilitaryUnit>
            {
                new() { Id = Guid.NewGuid(), ArmyId = ArmyId, UnitTypeId = UnitTypeId, Quantity = 5 },
            }
        };
        _armiesMock.Setup(a => a.GetArmyWithUnitsAsync(ArmyId)).ReturnsAsync(army);

        var sourceTile = new Tile { Id = TileId, KingdomId = KingdomId, CoordQ = 0, CoordR = 0 };
        var targetTile = new Tile { Id = TargetTileId, KingdomId = KingdomId, CoordQ = 1, CoordR = 0 };
        _tilesMock.Setup(t => t.GetByIdAsync(TileId)).ReturnsAsync(sourceTile);
        _tilesMock.Setup(t => t.GetByIdAsync(TargetTileId)).ReturnsAsync(targetTile);

        _armiesMock.Setup(a => a.GetArmyOnTileForKingdomAsync(TargetTileId, KingdomId)).ReturnsAsync((Army?)null);
        _armiesMock.Setup(a => a.GetEnemyArmyOnTileAsync(TargetTileId, KingdomId)).ReturnsAsync((Army?)null);
    }

    // -------------------------------------------------------------------------
    // TrainTroops tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TrainTroops_GuardFails_ReturnsError()
    {
        SetupGuardFailure("It is not your turn.");

        var result = await _sut.TrainTroopsAsync(GameId, UserId, DefaultTrainRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("It is not your turn.");
    }

    [Fact]
    public async Task TrainTroops_BuildingNotFound_ReturnsError()
    {
        SetupGuardSuccess();
        _buildingsMock.Setup(b => b.GetByIdAsync(BuildingId)).ReturnsAsync((Building?)null);

        var result = await _sut.TrainTroopsAsync(GameId, UserId, DefaultTrainRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Building not found.");
    }

    [Fact]
    public async Task TrainTroops_UnitTypeNotFound_ReturnsError()
    {
        SetupGuardSuccess();

        var building = new Building { Id = BuildingId, TileId = TileId, BuildingTypeId = BuildingTypeId };
        _buildingsMock.Setup(b => b.GetByIdAsync(BuildingId)).ReturnsAsync(building);
        _tilesMock.Setup(t => t.GetByIdAsync(TileId)).ReturnsAsync(new Tile { Id = TileId, KingdomId = KingdomId });
        _buildingUnitTypesMock.Setup(b => b.GetByBuildingAndUnitTypeAsync(BuildingTypeId, UnitTypeId))
            .ReturnsAsync(new BuildingUnitType { BuildingTypeId = BuildingTypeId, UnitTypeId = UnitTypeId });
        _unitTypesMock.Setup(u => u.GetByIdAsync(UnitTypeId)).ReturnsAsync((UnitType?)null);

        var result = await _sut.TrainTroopsAsync(GameId, UserId, DefaultTrainRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Unit type not found.");
    }

    [Fact]
    public async Task TrainTroops_HappyPath_ReturnsDto()
    {
        SetupTrainHappyPath();

        var result = await _sut.TrainTroopsAsync(GameId, UserId, DefaultTrainRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.TileId.ShouldBe(TileId);
        result.Value.UnitTypeId.ShouldBe(UnitTypeId);
        result.Value.QuantityTrained.ShouldBe(3);
        result.Value.TotalQuantity.ShouldBe(3);
        result.Value.KingdomId.ShouldBe(KingdomId);

        // Army added
        _addedArmies.Count.ShouldBe(1);

        // TurnLog created
        _addedTurnLogs.Count.ShouldBe(1);
        _addedTurnLogs[0].Action.ShouldBe("Train");
        _addedTurnLogs[0].GameId.ShouldBe(GameId);

        // Resources deducted: gold = 200 - 30, food = 200 - 15
        result.Value.ResourcesAfter["Gold"].ShouldBe(170);
        result.Value.ResourcesAfter["Food"].ShouldBe(185);
    }

    // -------------------------------------------------------------------------
    // MoveArmy tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task MoveArmy_GuardFails_ReturnsError()
    {
        SetupGuardFailure("Game not found.");

        var result = await _sut.MoveArmyAsync(GameId, UserId, DefaultMoveRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Game not found.");
    }

    [Fact]
    public async Task MoveArmy_ArmyNotFound_ReturnsError()
    {
        SetupGuardSuccess();
        _armiesMock.Setup(a => a.GetArmyWithUnitsAsync(ArmyId)).ReturnsAsync((Army?)null);

        var result = await _sut.MoveArmyAsync(GameId, UserId, DefaultMoveRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Army not found.");
    }

    [Fact]
    public async Task MoveArmy_HappyPath_ReturnsDto()
    {
        SetupMoveHappyPath();

        var result = await _sut.MoveArmyAsync(GameId, UserId, DefaultMoveRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.FromTileId.ShouldBe(TileId);
        result.Value.ToTileId.ShouldBe(TargetTileId);
        result.Value.KingdomId.ShouldBe(KingdomId);
        result.Value.TileClaimed.ShouldBeFalse();
        result.Value.ArmyMerged.ShouldBeFalse();

        // Army updated (TileId changed)
        _armiesMock.Verify(a => a.UpdateAsync(It.IsAny<Army>()), Times.Once);

        // TurnLog created
        _addedTurnLogs.Count.ShouldBe(1);
        _addedTurnLogs[0].Action.ShouldBe("Move");
    }

    // -------------------------------------------------------------------------
    // AttackAsync tests
    // -------------------------------------------------------------------------

    private static readonly Guid AttackerArmyId = Guid.Parse("aaaaaaaa-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid DefenderTileId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccee");
    private static readonly Guid Kingdom2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SwordsmanTypeId = Guid.Parse("cccccccc-0001-0000-0000-000000000001");
    private static readonly Guid ArcherTypeId = Guid.Parse("cccccccc-0001-0000-0000-000000000002");

    private static AttackRequest DefaultAttackRequest() => new()
    {
        AttackerArmyId = AttackerArmyId,
        DefenderTileId = DefenderTileId,
    };

    private void SetupAttackHappyPath()
    {
        SetupGuardSuccess();

        var swordsmanType = new UnitType { Id = SwordsmanTypeId, Name = new LangStr("Swordsman", "en"), BaseStrength = 10 };
        var archerType = new UnitType { Id = ArcherTypeId, Name = new LangStr("Archer", "en"), BaseStrength = 8 };

        var attackerArmy = new Army
        {
            Id = AttackerArmyId, TileId = TileId, KingdomId = KingdomId,
            Units = new List<MilitaryUnit>
            {
                new() { Id = Guid.NewGuid(), ArmyId = AttackerArmyId, UnitTypeId = SwordsmanTypeId, Quantity = 10, UnitType = swordsmanType },
            }
        };
        _armiesMock.Setup(a => a.GetArmyWithUnitsAsync(AttackerArmyId)).ReturnsAsync(attackerArmy);

        var attackerTile = new Tile { Id = TileId, KingdomId = KingdomId, CoordQ = 0, CoordR = 0, TerrainTypeId = Guid.NewGuid() };
        var defenderTile = new Tile { Id = DefenderTileId, KingdomId = Kingdom2Id, CoordQ = 1, CoordR = 0, TerrainTypeId = Guid.NewGuid() };
        _tilesMock.Setup(t => t.GetByIdAsync(TileId)).ReturnsAsync(attackerTile);
        _tilesMock.Setup(t => t.GetByIdAsync(DefenderTileId)).ReturnsAsync(defenderTile);

        var defenderArmy = new Army
        {
            Id = Guid.NewGuid(), TileId = DefenderTileId, KingdomId = Kingdom2Id,
            Units = new List<MilitaryUnit>
            {
                new() { Id = Guid.NewGuid(), UnitTypeId = ArcherTypeId, Quantity = 10, UnitType = archerType },
            }
        };
        _armiesMock.Setup(a => a.GetEnemyArmyOnTileAsync(DefenderTileId, KingdomId)).ReturnsAsync(defenderArmy);

        // Matchups: Swordsman vs Archer = 1.25, Archer vs Swordsman = 0.75
        var matchups = new List<UnitTypeMatchup>
        {
            new() { AttackerTypeId = SwordsmanTypeId, DefenderTypeId = ArcherTypeId, Multiplier = 1.25m },
            new() { AttackerTypeId = ArcherTypeId, DefenderTypeId = SwordsmanTypeId, Multiplier = 0.75m },
        };
        _unitTypeMatchupsMock.Setup(m => m.GetAllMatchupsAsync()).ReturnsAsync(matchups);

        // Kingdoms for faction lookup
        var attackerKingdom = new Kingdom { Id = KingdomId, FactionTypeId = FactionId };
        var defenderKingdom = new Kingdom { Id = Kingdom2Id, FactionTypeId = FactionId };
        _kingdomsMock.Setup(k => k.GetByIdAsync(KingdomId)).ReturnsAsync(attackerKingdom);
        _kingdomsMock.Setup(k => k.GetByIdAsync(Kingdom2Id)).ReturnsAsync(defenderKingdom);

        // No faction bonuses
        _factionUnitBonusesMock.Setup(f => f.GetBonusesForFactionAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<FactionUnitBonus>());

        // Terrain: Plains (0.0 defense)
        var terrain = new TerrainType { Id = defenderTile.TerrainTypeId, Name = new LangStr("Plains", "en"), DefenseBonus = 0.0m };
        _terrainTypesMock.Setup(t => t.GetByIdAsync(defenderTile.TerrainTypeId)).ReturnsAsync(terrain);

        // Delete/Update mocks
        _unitsMock.Setup(u => u.DeleteAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        _armiesMock.Setup(a => a.DeleteAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Attack_GuardFails_ReturnsError()
    {
        SetupGuardFailure("It is not your turn.");

        var result = await _sut.AttackAsync(GameId, UserId, DefaultAttackRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("It is not your turn.");
    }

    [Fact]
    public async Task Attack_ArmyNotFound_ReturnsError()
    {
        SetupGuardSuccess();
        _armiesMock.Setup(a => a.GetArmyWithUnitsAsync(AttackerArmyId)).ReturnsAsync((Army?)null);

        var result = await _sut.AttackAsync(GameId, UserId, DefaultAttackRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Attacker army not found.");
    }

    [Fact]
    public async Task Attack_NoEnemyOnTile_ReturnsError()
    {
        SetupGuardSuccess();

        var attackerArmy = new Army
        {
            Id = AttackerArmyId, TileId = TileId, KingdomId = KingdomId,
            Units = new List<MilitaryUnit> { new() { UnitTypeId = SwordsmanTypeId, Quantity = 5 } }
        };
        _armiesMock.Setup(a => a.GetArmyWithUnitsAsync(AttackerArmyId)).ReturnsAsync(attackerArmy);

        var attackerTile = new Tile { Id = TileId, KingdomId = KingdomId, CoordQ = 0, CoordR = 0 };
        var defenderTile = new Tile { Id = DefenderTileId, KingdomId = null, CoordQ = 1, CoordR = 0 };
        _tilesMock.Setup(t => t.GetByIdAsync(TileId)).ReturnsAsync(attackerTile);
        _tilesMock.Setup(t => t.GetByIdAsync(DefenderTileId)).ReturnsAsync(defenderTile);

        _armiesMock.Setup(a => a.GetEnemyArmyOnTileAsync(DefenderTileId, KingdomId)).ReturnsAsync((Army?)null);

        var result = await _sut.AttackAsync(GameId, UserId, DefaultAttackRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("No enemy army on target tile.");
    }

    [Fact]
    public async Task Attack_HappyPath_ReturnsCombatResolvedDto()
    {
        SetupAttackHappyPath();

        var result = await _sut.AttackAsync(GameId, UserId, DefaultAttackRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.AttackerKingdomId.ShouldBe(KingdomId);
        result.Value.DefenderKingdomId.ShouldBe(Kingdom2Id);
        result.Value.WinnerKingdomId.ShouldBe(KingdomId); // attacker wins (swordsmen beat archers)
        result.Value.TileCaptured.ShouldBeTrue();
        result.Value.AttackerStrength.ShouldBe(125m);
        result.Value.DefenderStrength.ShouldBe(60m);

        // Battle record persisted
        _addedBattles.Count.ShouldBe(1);

        // TurnLog created
        _addedTurnLogs.Count.ShouldBe(1);
        _addedTurnLogs[0].Action.ShouldBe("Attack");

        // Commit called
        _unitOfWorkMock.Verify(u => u.CommitAsync(default), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Phase 13: Elimination + win condition tests
    // -------------------------------------------------------------------------

    private static readonly Guid CapitalTileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccaf0");

    private void SetupEliminationAttackPath(bool isCapital, bool onlyOneKingdomRemains)
    {
        SetupGuardSuccess();

        // Override game to be Elimination win condition
        var game = new Game
        {
            Id = GameId, Status = EGameStatus.InProgress, TurnNumber = 1,
            CurrentTurnKingdomId = KingdomId,
            WinCondition = EWinCondition.Elimination
        };
        var kingdom = new Kingdom { Id = KingdomId, GameId = GameId, AppUserId = UserId, FactionTypeId = FactionId };
        _gameGuardMock.Setup(g => g.ValidateAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Ok(new GameGuardContext(game, kingdom)));

        var swordsmanType = new UnitType { Id = SwordsmanTypeId, Name = new LangStr("Swordsman", "en"), BaseStrength = 10 };
        var archerType = new UnitType { Id = ArcherTypeId, Name = new LangStr("Archer", "en"), BaseStrength = 8 };

        var attackerArmy = new Army
        {
            Id = AttackerArmyId, TileId = TileId, KingdomId = KingdomId,
            Units = new List<MilitaryUnit>
            {
                new() { Id = Guid.NewGuid(), ArmyId = AttackerArmyId, UnitTypeId = SwordsmanTypeId, Quantity = 50, UnitType = swordsmanType },
            }
        };
        _armiesMock.Setup(a => a.GetArmyWithUnitsAsync(AttackerArmyId)).ReturnsAsync(attackerArmy);

        var attackerTile = new Tile { Id = TileId, KingdomId = KingdomId, CoordQ = 0, CoordR = 0, TerrainTypeId = Guid.NewGuid() };
        var defenderTile = new Tile { Id = CapitalTileId, KingdomId = Kingdom2Id, CoordQ = 1, CoordR = 0, TerrainTypeId = Guid.NewGuid(), IsCapital = isCapital };
        _tilesMock.Setup(t => t.GetByIdAsync(TileId)).ReturnsAsync(attackerTile);
        _tilesMock.Setup(t => t.GetByIdAsync(CapitalTileId)).ReturnsAsync(defenderTile);

        var defenderArmy = new Army
        {
            Id = Guid.NewGuid(), TileId = CapitalTileId, KingdomId = Kingdom2Id,
            Units = new List<MilitaryUnit>
            {
                new() { Id = Guid.NewGuid(), UnitTypeId = ArcherTypeId, Quantity = 1, UnitType = archerType },
            }
        };
        _armiesMock.Setup(a => a.GetEnemyArmyOnTileAsync(CapitalTileId, KingdomId)).ReturnsAsync(defenderArmy);

        var matchups = new List<UnitTypeMatchup>
        {
            new() { AttackerTypeId = SwordsmanTypeId, DefenderTypeId = ArcherTypeId, Multiplier = 1.25m },
            new() { AttackerTypeId = ArcherTypeId, DefenderTypeId = SwordsmanTypeId, Multiplier = 0.75m },
        };
        _unitTypeMatchupsMock.Setup(m => m.GetAllMatchupsAsync()).ReturnsAsync(matchups);

        var attackerKingdomEntity = new Kingdom { Id = KingdomId, FactionTypeId = FactionId };
        var defenderKingdomEntity = new Kingdom { Id = Kingdom2Id, FactionTypeId = FactionId };
        _kingdomsMock.Setup(k => k.GetByIdAsync(KingdomId)).ReturnsAsync(attackerKingdomEntity);
        _kingdomsMock.Setup(k => k.GetByIdAsync(Kingdom2Id)).ReturnsAsync(defenderKingdomEntity);
        _kingdomsMock.Setup(k => k.UpdateAsync(It.IsAny<Kingdom>())).ReturnsAsync((Kingdom k) => k);

        _factionUnitBonusesMock.Setup(f => f.GetBonusesForFactionAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<FactionUnitBonus>());

        var terrain = new TerrainType { Id = defenderTile.TerrainTypeId, Name = new LangStr("Plains", "en"), DefenseBonus = 0.0m };
        _terrainTypesMock.Setup(t => t.GetByIdAsync(defenderTile.TerrainTypeId)).ReturnsAsync(terrain);

        _unitsMock.Setup(u => u.DeleteAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        _armiesMock.Setup(a => a.DeleteAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);

        if (isCapital)
        {
            // Setup elimination cleanup
            _tilesMock.Setup(t => t.GetTilesForKingdomAsync(Kingdom2Id))
                .ReturnsAsync(new List<Tile> { defenderTile });

            var defenderRemainingArmy = new Army { Id = Guid.NewGuid(), KingdomId = Kingdom2Id, Units = new List<MilitaryUnit>() };
            _armiesMock.Setup(a => a.GetArmiesWithUnitsForKingdomAsync(Kingdom2Id))
                .ReturnsAsync(new List<Army> { defenderRemainingArmy });

            // All kingdoms post-elimination: attacker active, defender eliminated
            var updatedDefenderKingdom = new Kingdom { Id = Kingdom2Id, IsEliminated = true };
            var allKingdoms = onlyOneKingdomRemains
                ? new List<Kingdom> { new() { Id = KingdomId, IsEliminated = false }, updatedDefenderKingdom }
                : new List<Kingdom> { new() { Id = KingdomId, IsEliminated = false }, updatedDefenderKingdom, new() { Id = Guid.NewGuid(), IsEliminated = false } };

            _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(allKingdoms);
            _tilesMock.Setup(t => t.GetTilesWithBuildingsForGameAsync(GameId)).ReturnsAsync(new List<Domain.Map.Tile>());

            // Real IServiceProvider resolves EliminationChecker via keyed registration
        }
    }

    [Fact]
    public async Task AttackAsync_WhenCapitalCaptured_EliminatesKingdom()
    {
        // Arrange: 1 active kingdom remains after elimination => game ends
        SetupEliminationAttackPath(isCapital: true, onlyOneKingdomRemains: true);

        // Act
        var request = new AttackRequest { AttackerArmyId = AttackerArmyId, DefenderTileId = CapitalTileId };
        var result = await _sut.AttackAsync(GameId, UserId, request);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.TileCaptured.ShouldBeTrue();

        // Defender kingdom marked eliminated
        _kingdomsMock.Verify(k => k.UpdateAsync(It.Is<Kingdom>(k => k.Id == Kingdom2Id && k.IsEliminated)), Times.Once);

        // Defender tiles nullified
        _tilesMock.Verify(t => t.GetTilesForKingdomAsync(Kingdom2Id), Times.Once);

        // Defender armies loaded for deletion
        _armiesMock.Verify(a => a.GetArmiesWithUnitsForKingdomAsync(Kingdom2Id), Times.Once);

        // GameOver populated (only 1 kingdom remains = game ends)
        result.Value.GameOver.ShouldNotBeNull();
        result.Value.GameOver!.WinnerKingdomId.ShouldBe(KingdomId);
    }

    [Fact]
    public async Task AttackAsync_WhenNonCapitalCaptured_NoElimination()
    {
        // Arrange: non-capital tile captured
        SetupEliminationAttackPath(isCapital: false, onlyOneKingdomRemains: false);

        // Act
        var request = new AttackRequest { AttackerArmyId = AttackerArmyId, DefenderTileId = CapitalTileId };
        var result = await _sut.AttackAsync(GameId, UserId, request);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();

        // No elimination: defender kingdoms should not be updated as eliminated
        _kingdomsMock.Verify(k => k.UpdateAsync(It.Is<Kingdom>(k => k.IsEliminated)), Times.Never);

        // No tile cleanup called
        _tilesMock.Verify(t => t.GetTilesForKingdomAsync(It.IsAny<Guid>()), Times.Never);

        // GameOver is null (no win condition check)
        result.Value.GameOver.ShouldBeNull();
    }
}
