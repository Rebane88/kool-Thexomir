using Application.Contracts;
using Application.Services.Building;
using Application.Services.Building.DTOs;
using Base;
using Domain.Buildings;
using Domain.Factions;
using Domain.Game;
using Domain.Map;
using Domain.Resources;
using Moq;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

public class BuildingServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGameGuard> _gameGuardMock = new();
    private readonly Mock<ITileRepository> _tilesMock = new();
    private readonly Mock<IBuildingRepository> _buildingsMock = new();
    private readonly Mock<IBuildingTypeRepository> _buildingTypesMock = new();
    private readonly Mock<IKingdomResourceRepository> _kingdomResourcesMock = new();
    private readonly Mock<IFactionTypeRepository> _factionTypesMock = new();
    private readonly Mock<ITurnLogRepository> _turnLogsMock = new();
    private readonly BuildingService _sut;

    // Captured entities
    private readonly List<Domain.Buildings.Building> _addedBuildings = [];
    private readonly List<TurnLog> _addedTurnLogs = [];

    // Well-known IDs
    private static readonly Guid GameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid KingdomId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserId = Guid.Parse("eeeeeeee-1111-1111-1111-111111111111");
    private static readonly Guid TileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid BuildingTypeId = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000001");
    private static readonly Guid FactionId = Guid.Parse("eeeeeeee-0001-0000-0000-000000000001");
    private static readonly Guid PrereqBuildingTypeId = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000100");

    public BuildingServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Tiles).Returns(_tilesMock.Object);
        _unitOfWorkMock.Setup(u => u.Buildings).Returns(_buildingsMock.Object);
        _unitOfWorkMock.Setup(u => u.BuildingTypes).Returns(_buildingTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.KingdomResources).Returns(_kingdomResourcesMock.Object);
        _unitOfWorkMock.Setup(u => u.FactionTypes).Returns(_factionTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.TurnLogs).Returns(_turnLogsMock.Object);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        _buildingsMock.Setup(b => b.AddAsync(It.IsAny<Domain.Buildings.Building>()))
            .ReturnsAsync((Domain.Buildings.Building b) => { _addedBuildings.Add(b); return b; });
        _turnLogsMock.Setup(t => t.AddAsync(It.IsAny<TurnLog>()))
            .ReturnsAsync((TurnLog t) => { _addedTurnLogs.Add(t); return t; });

        _sut = new BuildingService(_unitOfWorkMock.Object, _gameGuardMock.Object);
    }

    private PlaceBuildingRequest DefaultRequest() => new()
    {
        TileId = TileId,
        BuildingTypeId = BuildingTypeId,
    };

    private void SetupGuardSuccess()
    {
        var game = new Game { Id = GameId, Status = EGameStatus.InProgress, TurnNumber = 1 };
        var kingdom = new Kingdom { Id = KingdomId, GameId = GameId, AppUserId = UserId, FactionTypeId = FactionId };
        _gameGuardMock.Setup(g => g.ValidateAsync(GameId, UserId))
            .ReturnsAsync(Base.Contracts.Result<GameGuardContext>.Ok(new GameGuardContext(game, kingdom)));
    }

    private Tile CreateOwnedTile() => new()
    {
        Id = TileId,
        KingdomId = KingdomId,
        GameId = GameId,
    };

    private BuildingType CreateBuildingType(int goldCost = 100, int woodCost = 0, int stoneCost = 0, int manaCost = 0) => new()
    {
        Id = BuildingTypeId,
        Name = new LangStr("Lumber Mill", "en"),
        Tier = 1,
        Chain = "economy",
        GoldCost = goldCost,
        WoodCost = woodCost,
        StoneCost = stoneCost,
        ManaCost = manaCost,
    };

    private FactionType CreateFaction(decimal buildingCostModifier = 1.0m) => new()
    {
        Id = FactionId,
        Name = new LangStr("Iron Throne", "en"),
        BuildingCostModifier = buildingCostModifier,
    };

    private List<KingdomResource> CreateResources(int gold = 200, int wood = 200, int stone = 200, int mana = 200) =>
    [
        new() { KingdomId = KingdomId, ResourceType = EResourceType.Gold, Amount = gold },
        new() { KingdomId = KingdomId, ResourceType = EResourceType.Food, Amount = 200 },
        new() { KingdomId = KingdomId, ResourceType = EResourceType.Wood, Amount = wood },
        new() { KingdomId = KingdomId, ResourceType = EResourceType.Stone, Amount = stone },
        new() { KingdomId = KingdomId, ResourceType = EResourceType.Mana, Amount = mana },
    ];

    private void SetupHappyPath(int goldCost = 100, decimal costModifier = 1.0m, int goldAvailable = 200)
    {
        SetupGuardSuccess();
        _tilesMock.Setup(t => t.GetByIdAsync(TileId)).ReturnsAsync(CreateOwnedTile());
        _buildingsMock.Setup(b => b.GetBuildingsForKingdomAsync(KingdomId)).ReturnsAsync([]);
        _buildingTypesMock.Setup(bt => bt.GetByIdAsync(BuildingTypeId)).ReturnsAsync(CreateBuildingType(goldCost: goldCost));
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId)).ReturnsAsync(CreateFaction(costModifier));
        _kingdomResourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(KingdomId)).ReturnsAsync(CreateResources(gold: goldAvailable));
        _kingdomResourcesMock.Setup(r => r.UpdateAsync(It.IsAny<KingdomResource>()))
            .ReturnsAsync((KingdomResource r) => r);
    }

    // -------------------------------------------------------------------------
    // Test 1: Guard fails
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Build_GuardFails_ReturnsFailure()
    {
        _gameGuardMock.Setup(g => g.ValidateAsync(GameId, UserId))
            .ReturnsAsync(Base.Contracts.Result<GameGuardContext>.Fail("Game not found."));

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, DefaultRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Game not found.");
    }

    // -------------------------------------------------------------------------
    // Test 2: Tile not found
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Build_TileNotFound_ReturnsFailure()
    {
        SetupGuardSuccess();
        _tilesMock.Setup(t => t.GetByIdAsync(TileId)).ReturnsAsync((Tile?)null);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, DefaultRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Tile not found.");
    }

    // -------------------------------------------------------------------------
    // Test 3: Tile not owned
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Build_TileNotOwned_ReturnsFailure()
    {
        SetupGuardSuccess();
        var tile = CreateOwnedTile();
        tile.KingdomId = Guid.NewGuid(); // different kingdom
        _tilesMock.Setup(t => t.GetByIdAsync(TileId)).ReturnsAsync(tile);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, DefaultRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("do not own");
    }

    // -------------------------------------------------------------------------
    // Test 4: Tile already has a building
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Build_TileAlreadyHasBuilding_ReturnsFailure()
    {
        SetupGuardSuccess();
        _tilesMock.Setup(t => t.GetByIdAsync(TileId)).ReturnsAsync(CreateOwnedTile());
        _buildingsMock.Setup(b => b.GetBuildingsForKingdomAsync(KingdomId))
            .ReturnsAsync([new Domain.Buildings.Building { TileId = TileId, BuildingTypeId = BuildingTypeId }]);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, DefaultRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("already has a building");
    }

    // -------------------------------------------------------------------------
    // Test 5: Building type not found
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Build_BuildingTypeNotFound_ReturnsFailure()
    {
        SetupGuardSuccess();
        _tilesMock.Setup(t => t.GetByIdAsync(TileId)).ReturnsAsync(CreateOwnedTile());
        _buildingsMock.Setup(b => b.GetBuildingsForKingdomAsync(KingdomId)).ReturnsAsync([]);
        _buildingTypesMock.Setup(bt => bt.GetByIdAsync(BuildingTypeId)).ReturnsAsync((BuildingType?)null);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, DefaultRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Building type not found.");
    }

    // -------------------------------------------------------------------------
    // Test 6: Prerequisite not met
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Build_PrerequisiteNotMet_ReturnsFailure()
    {
        SetupGuardSuccess();
        _tilesMock.Setup(t => t.GetByIdAsync(TileId)).ReturnsAsync(CreateOwnedTile());
        _buildingsMock.Setup(b => b.GetBuildingsForKingdomAsync(KingdomId)).ReturnsAsync([]);

        var buildingType = CreateBuildingType();
        buildingType.PrerequisiteBuildingTypeId = PrereqBuildingTypeId;
        _buildingTypesMock.Setup(bt => bt.GetByIdAsync(BuildingTypeId)).ReturnsAsync(buildingType);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, DefaultRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("prerequisite");
    }

    // -------------------------------------------------------------------------
    // Test 7: Insufficient gold
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Build_InsufficientGold_ReturnsFailure()
    {
        SetupHappyPath(goldCost: 100, goldAvailable: 50);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, DefaultRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Not enough Gold");
    }

    // -------------------------------------------------------------------------
    // Test 8: No partial deduction on failure
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Build_InsufficientResources_NoPartialDeduction()
    {
        SetupGuardSuccess();
        _tilesMock.Setup(t => t.GetByIdAsync(TileId)).ReturnsAsync(CreateOwnedTile());
        _buildingsMock.Setup(b => b.GetBuildingsForKingdomAsync(KingdomId)).ReturnsAsync([]);
        _buildingTypesMock.Setup(bt => bt.GetByIdAsync(BuildingTypeId))
            .ReturnsAsync(CreateBuildingType(goldCost: 100, woodCost: 100));
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId)).ReturnsAsync(CreateFaction());

        var resources = CreateResources(gold: 200, wood: 10); // gold OK, wood insufficient
        _kingdomResourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(KingdomId)).ReturnsAsync(resources);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, DefaultRequest());

        result.IsSuccess.ShouldBeFalse();
        // Verify no partial deductions happened
        resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(200);
        resources.Single(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(10);
    }

    // -------------------------------------------------------------------------
    // Test 9: Happy path
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Build_HappyPath_CreatesBuilding()
    {
        SetupHappyPath(goldCost: 100, goldAvailable: 200);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, DefaultRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.TileId.ShouldBe(TileId);
        result.Value.BuildingTypeId.ShouldBe(BuildingTypeId);
        result.Value.KingdomId.ShouldBe(KingdomId);

        // Building created
        _addedBuildings.Count.ShouldBe(1);
        _addedBuildings[0].TileId.ShouldBe(TileId);
        _addedBuildings[0].BuildingTypeId.ShouldBe(BuildingTypeId);

        // Resources deducted
        result.Value.ResourcesAfter["Gold"].ShouldBe(100); // 200 - 100

        // TurnLog created
        _addedTurnLogs.Count.ShouldBe(1);
        _addedTurnLogs[0].Action.ShouldBe("Build");
        _addedTurnLogs[0].GameId.ShouldBe(GameId);
        _addedTurnLogs[0].KingdomId.ShouldBe(KingdomId);
    }

    // -------------------------------------------------------------------------
    // Test 10: Faction cost modifier
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Build_FactionCostModifier_Applied()
    {
        SetupHappyPath(goldCost: 100, costModifier: 0.8m, goldAvailable: 200);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, DefaultRequest());

        result.IsSuccess.ShouldBeTrue();
        // floor(100 * 0.8) = 80, so 200 - 80 = 120
        result.Value!.ResourcesAfter["Gold"].ShouldBe(120);
    }
}
