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
    private readonly MilitaryService _sut;

    // Captured entities
    private readonly List<Army> _addedArmies = [];
    private readonly List<MilitaryUnit> _addedUnits = [];
    private readonly List<TurnLog> _addedTurnLogs = [];

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

        _sut = new MilitaryService(_unitOfWorkMock.Object, _gameGuardMock.Object);
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
}
