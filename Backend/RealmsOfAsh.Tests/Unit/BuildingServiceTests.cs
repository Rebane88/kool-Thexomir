using Application.Contracts;
using Application.Services.Building;
using Application.Services.Building.DTOs;
using Base;
using Base.Contracts;
using Domain.Buildings;
using Domain.Factions;
using Domain.Game;
using Domain.Map;
using Domain.Resources;
using Moq;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

[Trait("Category", "Unit")]
public class BuildingServiceTests
{
    // Mocks
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGameGuard> _gameGuardMock = new();
    private readonly Mock<IBuildingRepository> _buildingsMock = new();
    private readonly Mock<IBuildingTypeRepository> _buildingTypesMock = new();
    private readonly Mock<ITileRepository> _tilesMock = new();
    private readonly Mock<IKingdomResourceRepository> _resourcesMock = new();
    private readonly Mock<IFactionTypeRepository> _factionTypesMock = new();
    private readonly Mock<ITurnLogRepository> _turnLogsMock = new();
    private readonly BuildingService _sut;

    // Fixed IDs
    private static readonly Guid GameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid KingdomId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid OtherKingdomId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid FactionTypeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid TileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Tier1TypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Tier2TypeId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public BuildingServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Buildings).Returns(_buildingsMock.Object);
        _unitOfWorkMock.Setup(u => u.BuildingTypes).Returns(_buildingTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.Tiles).Returns(_tilesMock.Object);
        _unitOfWorkMock.Setup(u => u.KingdomResources).Returns(_resourcesMock.Object);
        _unitOfWorkMock.Setup(u => u.FactionTypes).Returns(_factionTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.TurnLogs).Returns(_turnLogsMock.Object);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        _sut = new BuildingService(_unitOfWorkMock.Object, _gameGuardMock.Object);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Game CreateGame() => new()
    {
        Id = GameId,
        Status = EGameStatus.InProgress,
        RoundNumber = 3,
        CurrentTurnKingdomId = KingdomId
    };

    private static Kingdom CreateKingdom() => new()
    {
        Id = KingdomId,
        GameId = GameId,
        AppUserId = UserId,
        FactionTypeId = FactionTypeId,
        Status = EKingdomStatus.Active
    };

    private static Tile CreateTile(Guid? kingdomId = null, bool isCastle = false, int q = 0, int r = 0) => new()
    {
        Id = TileId,
        GameId = GameId,
        KingdomId = kingdomId ?? KingdomId,
        IsCastle = isCastle,
        CoordQ = q,
        CoordR = r
    };

    private static BuildingType CreateBuildingType(int tier = 1, Guid? unlockedBy = null) => new()
    {
        Id = tier == 1 ? Tier1TypeId : Tier2TypeId,
        Name = new LangStr("Test Building", "en"),
        Tier = tier,
        Chain = "economic",
        CostGold = 50,
        CostFood = 0,
        CostWood = 30,
        CostStone = 0,
        CostMana = 0,
        UnlockedByBuildingTypeId = unlockedBy
    };

    private static Building CreateBuilding(Guid tileId, Guid buildingTypeId) => new()
    {
        Id = Guid.NewGuid(),
        TileId = tileId,
        BuildingTypeId = buildingTypeId,
        KingdomId = KingdomId,
        BuiltOnRound = 1,
        BuiltAt = DateTime.UtcNow
    };

    private static FactionType CreateFactionType(decimal buildingCostModifier = 1.0m) => new()
    {
        Id = FactionTypeId,
        Name = new LangStr("Test Faction", "en"),
        BuildingCostModifier = buildingCostModifier
    };

    private static List<KingdomResource> CreateResources(int gold = 500, int food = 500,
        int wood = 500, int stone = 500, int mana = 500) =>
    [
        new() { Id = Guid.NewGuid(), KingdomId = KingdomId, ResourceType = EResourceType.Gold, Amount = gold },
        new() { Id = Guid.NewGuid(), KingdomId = KingdomId, ResourceType = EResourceType.Food, Amount = food },
        new() { Id = Guid.NewGuid(), KingdomId = KingdomId, ResourceType = EResourceType.Wood, Amount = wood },
        new() { Id = Guid.NewGuid(), KingdomId = KingdomId, ResourceType = EResourceType.Stone, Amount = stone },
        new() { Id = Guid.NewGuid(), KingdomId = KingdomId, ResourceType = EResourceType.Mana, Amount = mana }
    ];

    private static PlaceBuildingRequest CreateRequest(Guid? buildingTypeId = null, Guid? tileId = null) => new()
    {
        BuildingTypeId = buildingTypeId ?? Tier1TypeId,
        TileId = tileId ?? TileId
    };

    private void SetupSuccessfulGuard()
    {
        var game = CreateGame();
        var kingdom = CreateKingdom();
        _gameGuardMock.Setup(g => g.ValidateAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Ok(new GameGuardContext(game, kingdom)));
    }

    private void SetupFullSuccessPath(BuildingType? buildingType = null, Tile? tile = null,
        List<Building>? buildings = null, FactionType? factionType = null,
        List<KingdomResource>? resources = null, List<Tile>? gameTiles = null)
    {
        SetupSuccessfulGuard();

        var bt = buildingType ?? CreateBuildingType();
        var t = tile ?? CreateTile();
        var f = factionType ?? CreateFactionType();
        var r = resources ?? CreateResources();
        var b = buildings ?? new List<Building>();
        var gt = gameTiles ?? new List<Tile> { t };

        _buildingTypesMock.Setup(x => x.GetByIdAsync(bt.Id)).ReturnsAsync(bt);
        _tilesMock.Setup(x => x.GetByIdAsync(t.Id)).ReturnsAsync(t);
        _buildingsMock.Setup(x => x.GetBuildingsForKingdomAsync(KingdomId)).ReturnsAsync(b);
        _factionTypesMock.Setup(x => x.GetByIdAsync(FactionTypeId)).ReturnsAsync(f);
        _resourcesMock.Setup(x => x.GetMutableResourcesForKingdomAsync(KingdomId)).ReturnsAsync(r);
        _tilesMock.Setup(x => x.GetTilesForGameAsync(GameId)).ReturnsAsync(gt);
    }

    // -------------------------------------------------------------------------
    // Failure tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PlaceBuilding_FailsWhenGameGuardFails()
    {
        _gameGuardMock.Setup(g => g.ValidateAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Fail("It is not your turn."));

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("not your turn");
    }

    [Fact]
    public async Task PlaceBuilding_FailsWhenBuildingTypeNotFound()
    {
        SetupSuccessfulGuard();
        _buildingTypesMock.Setup(x => x.GetByIdAsync(Tier1TypeId)).ReturnsAsync((BuildingType?)null);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Building type not found.");
    }

    [Fact]
    public async Task PlaceBuilding_FailsWhenTileNotFound()
    {
        SetupSuccessfulGuard();
        _buildingTypesMock.Setup(x => x.GetByIdAsync(Tier1TypeId)).ReturnsAsync(CreateBuildingType());
        _tilesMock.Setup(x => x.GetByIdAsync(TileId)).ReturnsAsync((Tile?)null);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Tile not found.");
    }

    [Fact]
    public async Task PlaceBuilding_FailsWhenTileInDifferentGame()
    {
        SetupSuccessfulGuard();
        _buildingTypesMock.Setup(x => x.GetByIdAsync(Tier1TypeId)).ReturnsAsync(CreateBuildingType());
        var tile = CreateTile();
        tile.GameId = Guid.NewGuid(); // different game
        _tilesMock.Setup(x => x.GetByIdAsync(TileId)).ReturnsAsync(tile);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Tile does not belong to this game.");
    }

    [Fact]
    public async Task PlaceBuilding_FailsWhenTileIsCastle()
    {
        var tile = CreateTile(isCastle: true);
        SetupFullSuccessPath(tile: tile);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("castle");
    }

    [Fact]
    public async Task PlaceBuilding_FailsWhenTier2WithoutPrerequisite()
    {
        var tier2Type = CreateBuildingType(tier: 2, unlockedBy: Tier1TypeId);
        SetupFullSuccessPath(buildingType: tier2Type);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest(buildingTypeId: Tier2TypeId));

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("no building to upgrade");
    }

    [Fact]
    public async Task PlaceBuilding_FailsWhenInsufficientResources()
    {
        var resources = CreateResources(gold: 0, wood: 0);
        SetupFullSuccessPath(resources: resources);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Insufficient");
    }

    // -------------------------------------------------------------------------
    // Success tests - Tier 1 placement
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PlaceBuilding_Tier1_CreatesBuilding()
    {
        SetupFullSuccessPath();

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsUpgrade.ShouldBeFalse();
        _buildingsMock.Verify(r => r.AddAsync(It.IsAny<Building>()), Times.Once);
    }

    [Fact]
    public async Task PlaceBuilding_Tier1_ClaimsAdjacentUnownedTiles()
    {
        var tile = CreateTile(q: 0, r: 0);
        // Create neighbor tiles: (1,0), (-1,0), (0,1), (0,-1), (1,-1), (-1,1)
        var neighbor1 = new Tile { Id = Guid.NewGuid(), GameId = GameId, CoordQ = 1, CoordR = 0, KingdomId = null };
        var neighbor2 = new Tile { Id = Guid.NewGuid(), GameId = GameId, CoordQ = -1, CoordR = 0, KingdomId = null };
        var neighbor3 = new Tile { Id = Guid.NewGuid(), GameId = GameId, CoordQ = 0, CoordR = 1, KingdomId = null };
        var gameTiles = new List<Tile> { tile, neighbor1, neighbor2, neighbor3 };

        SetupFullSuccessPath(tile: tile, gameTiles: gameTiles);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ClaimedTileIds.Count.ShouldBe(3);
        result.Value.ClaimedTileIds.ShouldContain(neighbor1.Id);
        result.Value.ClaimedTileIds.ShouldContain(neighbor2.Id);
        result.Value.ClaimedTileIds.ShouldContain(neighbor3.Id);
        _tilesMock.Verify(r => r.UpdateAsync(It.IsAny<Tile>()), Times.Exactly(3));
    }

    [Fact]
    public async Task PlaceBuilding_Tier1_DoesNotClaimEnemyTiles()
    {
        var tile = CreateTile(q: 0, r: 0);
        var enemyTile = new Tile
        {
            Id = Guid.NewGuid(), GameId = GameId, CoordQ = 1, CoordR = 0,
            KingdomId = OtherKingdomId // owned by enemy
        };
        var unownedTile = new Tile
        {
            Id = Guid.NewGuid(), GameId = GameId, CoordQ = -1, CoordR = 0,
            KingdomId = null // unowned
        };
        var gameTiles = new List<Tile> { tile, enemyTile, unownedTile };

        SetupFullSuccessPath(tile: tile, gameTiles: gameTiles);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ClaimedTileIds.Count.ShouldBe(1);
        result.Value.ClaimedTileIds.ShouldContain(unownedTile.Id);
        result.Value.ClaimedTileIds.ShouldNotContain(enemyTile.Id);
    }

    // -------------------------------------------------------------------------
    // Success tests - Tier 2 upgrade
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PlaceBuilding_Tier2_UpdatesExistingBuildingInPlace()
    {
        var tier1Type = CreateBuildingType(tier: 1);
        var tier2Type = CreateBuildingType(tier: 2, unlockedBy: Tier1TypeId);
        var existingBuilding = CreateBuilding(TileId, Tier1TypeId);
        var buildings = new List<Building> { existingBuilding };

        SetupFullSuccessPath(buildingType: tier2Type, buildings: buildings);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest(buildingTypeId: Tier2TypeId));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsUpgrade.ShouldBeTrue();
        result.Value.BuildingId.ShouldBe(existingBuilding.Id); // same entity
        existingBuilding.BuildingTypeId.ShouldBe(Tier2TypeId); // updated in place
        _buildingsMock.Verify(r => r.UpdateAsync(existingBuilding), Times.Once);
        _buildingsMock.Verify(r => r.AddAsync(It.IsAny<Building>()), Times.Never);
    }

    [Fact]
    public async Task PlaceBuilding_Tier2_DoesNotClaimTiles()
    {
        var tier2Type = CreateBuildingType(tier: 2, unlockedBy: Tier1TypeId);
        var existingBuilding = CreateBuilding(TileId, Tier1TypeId);
        var buildings = new List<Building> { existingBuilding };
        var tile = CreateTile(q: 0, r: 0);
        var neighbor = new Tile { Id = Guid.NewGuid(), GameId = GameId, CoordQ = 1, CoordR = 0, KingdomId = null };
        var gameTiles = new List<Tile> { tile, neighbor };

        SetupFullSuccessPath(buildingType: tier2Type, buildings: buildings, tile: tile, gameTiles: gameTiles);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest(buildingTypeId: Tier2TypeId));

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ClaimedTileIds.ShouldBeEmpty();
        _tilesMock.Verify(r => r.UpdateAsync(It.IsAny<Tile>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // TurnLog tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PlaceBuilding_CreatesConstructedTurnLog()
    {
        SetupFullSuccessPath();

        await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        _turnLogsMock.Verify(r => r.AddAsync(It.Is<TurnLog>(
            t => t.EventType == EEventType.BuildingConstructed)), Times.Once);
    }

    [Fact]
    public async Task PlaceBuilding_CreatesUpgradedTurnLog()
    {
        var tier2Type = CreateBuildingType(tier: 2, unlockedBy: Tier1TypeId);
        var existingBuilding = CreateBuilding(TileId, Tier1TypeId);
        var buildings = new List<Building> { existingBuilding };

        SetupFullSuccessPath(buildingType: tier2Type, buildings: buildings);

        await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest(buildingTypeId: Tier2TypeId));

        _turnLogsMock.Verify(r => r.AddAsync(It.Is<TurnLog>(
            t => t.EventType == EEventType.BuildingUpgraded)), Times.Once);
    }

    [Fact]
    public async Task PlaceBuilding_Tier1_CreatesTileCapturedTurnLog_WhenTilesClaimed()
    {
        var tile = CreateTile(q: 0, r: 0);
        var neighbor = new Tile { Id = Guid.NewGuid(), GameId = GameId, CoordQ = 1, CoordR = 0, KingdomId = null };
        var gameTiles = new List<Tile> { tile, neighbor };

        SetupFullSuccessPath(tile: tile, gameTiles: gameTiles);

        await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        _turnLogsMock.Verify(r => r.AddAsync(It.Is<TurnLog>(
            t => t.EventType == EEventType.TileCaptured)), Times.Once);
    }

    // -------------------------------------------------------------------------
    // DTO result tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PlaceBuilding_ReturnsClaimedTileIdsInDto()
    {
        var tile = CreateTile(q: 0, r: 0);
        var neighbor = new Tile { Id = Guid.NewGuid(), GameId = GameId, CoordQ = 1, CoordR = 0, KingdomId = null };
        var gameTiles = new List<Tile> { tile, neighbor };

        SetupFullSuccessPath(tile: tile, gameTiles: gameTiles);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ClaimedTileIds.ShouldNotBeEmpty();
        result.Value.ClaimedTileIds.ShouldContain(neighbor.Id);
    }

    [Fact]
    public async Task PlaceBuilding_ReturnsResourcesAfterInDto()
    {
        var resources = CreateResources(gold: 500, wood: 500);
        SetupFullSuccessPath(resources: resources);

        var result = await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ResourcesAfter.ShouldContainKey("Gold");
        result.Value.ResourcesAfter["Gold"].ShouldBe(450); // 500 - 50 cost
    }

    // -------------------------------------------------------------------------
    // Commit behavior tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PlaceBuilding_CommitsOnSuccess()
    {
        SetupFullSuccessPath();

        await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        _unitOfWorkMock.Verify(u => u.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task PlaceBuilding_DoesNotCommitOnFailure()
    {
        _gameGuardMock.Setup(g => g.ValidateAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Fail("It is not your turn."));

        await _sut.PlaceBuildingAsync(GameId, UserId, CreateRequest());

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
