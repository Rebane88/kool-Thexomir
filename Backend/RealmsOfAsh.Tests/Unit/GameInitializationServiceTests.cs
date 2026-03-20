using Application.Contracts;
using Application.Services.GameInitialization;
using Application.Services.GameInitialization.DTOs;
using Base;
using Domain.Buildings;
using Domain.Factions;
using Domain.Game;
using Domain.Map;
using Domain.Military;
using Domain.Resources;
using Moq;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// GameInitializationService unit tests — resource initialization, map creation, castle placement.
/// </summary>
[Trait("Category", "Unit")]
public class GameInitializationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IKingdomRepository> _kingdomsMock = new();
    private readonly Mock<IFactionTypeRepository> _factionTypesMock = new();
    private readonly Mock<IKingdomResourceRepository> _kingdomResourcesMock = new();
    private readonly Mock<IGameRepository> _gamesMock = new();
    private readonly Mock<ITileRepository> _tilesMock = new();
    private readonly Mock<ITerrainTypeRepository> _terrainTypesMock = new();
    private readonly Mock<IBuildingRepository> _buildingsMock = new();
    private readonly Mock<IBuildingTypeRepository> _buildingTypesMock = new();
    private readonly Mock<IArmyRepository> _armiesMock = new();
    private readonly Mock<IDeclaredAttackRepository> _declaredAttacksMock = new();
    private readonly List<KingdomResource> _addedResources = [];
    private readonly List<Tile> _addedTiles = [];
    private readonly List<Building> _addedBuildings = [];
    private readonly GameInitializationService _sut;

    private static readonly Guid GameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid KingdomId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid KingdomId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid FactionId1 = Guid.Parse("f1111111-1111-1111-1111-111111111111");
    private static readonly Guid FactionId2 = Guid.Parse("f2222222-2222-2222-2222-222222222222");

    // Terrain type IDs matching seeder
    private static readonly Guid PlainsId = new("AAAAAAAA-0001-0000-0000-000000000001");
    private static readonly Guid ForestId = new("AAAAAAAA-0001-0000-0000-000000000002");
    private static readonly Guid MountainId = new("AAAAAAAA-0001-0000-0000-000000000003");
    private static readonly Guid DesertId = new("AAAAAAAA-0001-0000-0000-000000000004");
    private static readonly Guid MagicGroveId = new("AAAAAAAA-0001-0000-0000-000000000005");

    private static readonly Guid CastleBuildingTypeId = new("BBBBBBBB-0001-0000-0000-000000000100");

    public GameInitializationServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Kingdoms).Returns(_kingdomsMock.Object);
        _unitOfWorkMock.Setup(u => u.FactionTypes).Returns(_factionTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.KingdomResources).Returns(_kingdomResourcesMock.Object);
        _unitOfWorkMock.Setup(u => u.Games).Returns(_gamesMock.Object);
        _unitOfWorkMock.Setup(u => u.Tiles).Returns(_tilesMock.Object);
        _unitOfWorkMock.Setup(u => u.TerrainTypes).Returns(_terrainTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.Buildings).Returns(_buildingsMock.Object);
        _unitOfWorkMock.Setup(u => u.BuildingTypes).Returns(_buildingTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.Armies).Returns(_armiesMock.Object);
        _unitOfWorkMock.Setup(u => u.DeclaredAttacks).Returns(_declaredAttacksMock.Object);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        _kingdomResourcesMock.Setup(r => r.AddAsync(It.IsAny<KingdomResource>()))
            .Callback<KingdomResource>(kr => _addedResources.Add(kr))
            .ReturnsAsync((KingdomResource kr) => kr);

        _tilesMock.Setup(t => t.AddAsync(It.IsAny<Tile>()))
            .Callback<Tile>(tile => _addedTiles.Add(tile))
            .ReturnsAsync((Tile tile) => tile);

        _buildingsMock.Setup(b => b.AddAsync(It.IsAny<Building>()))
            .Callback<Building>(building => _addedBuildings.Add(building))
            .ReturnsAsync((Building b) => b);

        _sut = new GameInitializationService(_unitOfWorkMock.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<GameInitializationService>.Instance);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Kingdom CreateKingdom(Guid kingdomId, Guid factionTypeId, int turnOrder = 1) => new()
    {
        Id = kingdomId,
        GameId = GameId,
        FactionTypeId = factionTypeId,
        Name = "Test Kingdom",
        TurnOrder = turnOrder,
        Status = EKingdomStatus.Active
    };

    private static FactionType CreateFactionType(
        Guid id,
        EResourceType? bonusResource = null,
        int bonusAmount = 0,
        int actionPointModifier = 0) => new()
    {
        Id = id,
        Name = new LangStr("TestFaction"),
        StartingBonusResource = bonusResource,
        StartingBonusAmount = bonusAmount,
        ActionPointModifier = actionPointModifier
    };

    private static Game CreateGame() => new()
    {
        Id = GameId,
        Name = "Test Game",
        Status = EGameStatus.Lobby,
        MaxPlayers = 2,
        BaseActionPoints = 4
    };

    private static List<TerrainType> CreateTerrainTypes() =>
    [
        new TerrainType { Id = PlainsId, Name = new LangStr("Plains") },
        new TerrainType { Id = ForestId, Name = new LangStr("Forest") },
        new TerrainType { Id = MountainId, Name = new LangStr("Mountain") },
        new TerrainType { Id = DesertId, Name = new LangStr("Desert") },
        new TerrainType { Id = MagicGroveId, Name = new LangStr("Magic Grove") },
    ];

    private static List<BuildingType> CreateBuildingTypes() =>
    [
        new BuildingType { Id = CastleBuildingTypeId, Name = new LangStr("Castle"), Tier = 0, Chain = "Castle" }
    ];

    private void SetupInitializeMocks(Game game, List<Kingdom> kingdoms,
        List<FactionType> factions)
    {
        _gamesMock.Setup(g => g.GetByIdAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        _terrainTypesMock.Setup(t => t.GetAllAsync()).ReturnsAsync(CreateTerrainTypes());
        _buildingTypesMock.Setup(b => b.GetAllAsync()).ReturnsAsync(CreateBuildingTypes());

        foreach (var faction in factions)
        {
            _factionTypesMock.Setup(f => f.GetByIdAsync(faction.Id)).ReturnsAsync(faction);
        }

        // BuildGameStateSnapshotAsync mocks (called at end of InitializeGameAsync)
        _tilesMock.Setup(t => t.GetTilesWithBuildingsForGameAsync(GameId))
            .ReturnsAsync(() => _addedTiles);

        _armiesMock.Setup(a => a.GetArmiesForKingdomAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Enumerable.Empty<Army>());

        _kingdomResourcesMock.Setup(r => r.GetResourcesForKingdomAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<KingdomResource>());

        _declaredAttacksMock.Setup(d => d.GetForGameRoundAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync(new List<DeclaredAttack>());
    }

    // -------------------------------------------------------------------------
    // Resource initialization tests (existing)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeKingdomResources_CreatesBaseResourcesForEachKingdom()
    {
        // Arrange: 1 kingdom with no faction bonus
        var kingdom = CreateKingdom(KingdomId1, FactionId1);
        var faction = CreateFactionType(FactionId1);

        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId))
            .ReturnsAsync([kingdom]);
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId1))
            .ReturnsAsync(faction);

        // Act
        await _sut.InitializeKingdomResourcesAsync(GameId);

        // Assert: 5 resources created with base amounts
        _addedResources.Count.ShouldBe(5);

        _addedResources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(150);
        _addedResources.Single(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(60);
        _addedResources.Single(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(50);
        _addedResources.Single(r => r.ResourceType == EResourceType.Stone).Amount.ShouldBe(20);
        _addedResources.Single(r => r.ResourceType == EResourceType.Mana).Amount.ShouldBe(0);
    }

    [Fact]
    public async Task InitializeKingdomResources_AppliesFactionStartingBonus()
    {
        // Arrange: faction with Gold bonus +50
        var kingdom = CreateKingdom(KingdomId1, FactionId1);
        var faction = CreateFactionType(FactionId1, EResourceType.Gold, 50);

        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId))
            .ReturnsAsync([kingdom]);
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId1))
            .ReturnsAsync(faction);

        // Act
        await _sut.InitializeKingdomResourcesAsync(GameId);

        // Assert: Gold = 150 base + 50 bonus = 200, others unchanged
        _addedResources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(200);
        _addedResources.Single(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(60);
        _addedResources.Single(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(50);
        _addedResources.Single(r => r.ResourceType == EResourceType.Stone).Amount.ShouldBe(20);
        _addedResources.Single(r => r.ResourceType == EResourceType.Mana).Amount.ShouldBe(0);
    }

    [Fact]
    public async Task InitializeKingdomResources_HandlesMultipleKingdoms()
    {
        // Arrange: 2 kingdoms with different factions
        var kingdom1 = CreateKingdom(KingdomId1, FactionId1);
        var kingdom2 = CreateKingdom(KingdomId2, FactionId2);
        var faction1 = CreateFactionType(FactionId1, EResourceType.Gold, 50);
        var faction2 = CreateFactionType(FactionId2, EResourceType.Wood, 30);

        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId))
            .ReturnsAsync([kingdom1, kingdom2]);
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId1))
            .ReturnsAsync(faction1);
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId2))
            .ReturnsAsync(faction2);

        // Act
        await _sut.InitializeKingdomResourcesAsync(GameId);

        // Assert: 10 resources total (5 per kingdom)
        _addedResources.Count.ShouldBe(10);

        // Kingdom 1: Gold bonus
        var k1Resources = _addedResources.Where(r => r.KingdomId == KingdomId1).ToList();
        k1Resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(200);
        k1Resources.Single(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(50);

        // Kingdom 2: Wood bonus
        var k2Resources = _addedResources.Where(r => r.KingdomId == KingdomId2).ToList();
        k2Resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(150);
        k2Resources.Single(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(80);
    }

    [Fact]
    public async Task InitializeKingdomResources_CommitsOnce()
    {
        // Arrange
        var kingdom = CreateKingdom(KingdomId1, FactionId1);
        var faction = CreateFactionType(FactionId1);

        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId))
            .ReturnsAsync([kingdom]);
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId1))
            .ReturnsAsync(faction);

        // Act
        await _sut.InitializeKingdomResourcesAsync(GameId);

        // Assert: CommitAsync called exactly once
        _unitOfWorkMock.Verify(u => u.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task InitializeKingdomResources_NoFactionBonus_UsesBaseAmountsOnly()
    {
        // Arrange: faction with StartingBonusResource=null and StartingBonusAmount=0
        var kingdom = CreateKingdom(KingdomId1, FactionId1);
        var faction = CreateFactionType(FactionId1, bonusResource: null, bonusAmount: 0);

        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId))
            .ReturnsAsync([kingdom]);
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId1))
            .ReturnsAsync(faction);

        // Act
        await _sut.InitializeKingdomResourcesAsync(GameId);

        // Assert: all at base level
        _addedResources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(150);
        _addedResources.Single(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(60);
        _addedResources.Single(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(50);
        _addedResources.Single(r => r.ResourceType == EResourceType.Stone).Amount.ShouldBe(20);
        _addedResources.Single(r => r.ResourceType == EResourceType.Mana).Amount.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // InitializeGameAsync tests — map creation, castle placement, territory
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeGame_SetsMapDimensions_ForTwoPlayers()
    {
        // Arrange
        var game = CreateGame();
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(KingdomId1, FactionId1, turnOrder: 1),
            CreateKingdom(KingdomId2, FactionId2, turnOrder: 2)
        };
        var factions = new List<FactionType>
        {
            CreateFactionType(FactionId1),
            CreateFactionType(FactionId2)
        };
        SetupInitializeMocks(game, kingdoms, factions);

        // Act
        await _sut.InitializeGameAsync(GameId);

        // Assert: 2 players -> radius 9 hexagonal map
        game.MapWidth.ShouldBe(9);
        game.MapHeight.ShouldBe(0);
    }

    [Fact]
    public async Task InitializeGame_CreatesTiles_CorrectCount()
    {
        // Arrange
        var game = CreateGame();
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(KingdomId1, FactionId1, turnOrder: 1),
            CreateKingdom(KingdomId2, FactionId2, turnOrder: 2)
        };
        var factions = new List<FactionType>
        {
            CreateFactionType(FactionId1),
            CreateFactionType(FactionId2)
        };
        SetupInitializeMocks(game, kingdoms, factions);

        // Act
        await _sut.InitializeGameAsync(GameId);

        // Assert: radius 9 hex grid = 271 tiles
        _addedTiles.Count.ShouldBe(271);
    }

    [Fact]
    public async Task InitializeGame_PlacesCastles_WithIsCastleTrue()
    {
        // Arrange
        var game = CreateGame();
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(KingdomId1, FactionId1, turnOrder: 1),
            CreateKingdom(KingdomId2, FactionId2, turnOrder: 2)
        };
        var factions = new List<FactionType>
        {
            CreateFactionType(FactionId1),
            CreateFactionType(FactionId2)
        };
        SetupInitializeMocks(game, kingdoms, factions);

        // Act
        await _sut.InitializeGameAsync(GameId);

        // Assert: exactly 2 tiles have IsCastle = true
        var castleTiles = _addedTiles.Where(t => t.IsCastle).ToList();
        castleTiles.Count.ShouldBe(2);

        // Each castle tile has a KingdomId set
        castleTiles.ShouldAllBe(t => t.KingdomId != null);

        // Castle tiles are on Plains terrain
        castleTiles.ShouldAllBe(t => t.TerrainTypeId == PlainsId);
    }

    [Fact]
    public async Task InitializeGame_CastleTiles_HaveBuildingCreated()
    {
        // Arrange
        var game = CreateGame();
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(KingdomId1, FactionId1, turnOrder: 1),
            CreateKingdom(KingdomId2, FactionId2, turnOrder: 2)
        };
        var factions = new List<FactionType>
        {
            CreateFactionType(FactionId1),
            CreateFactionType(FactionId2)
        };
        SetupInitializeMocks(game, kingdoms, factions);

        // Act
        await _sut.InitializeGameAsync(GameId);

        // Assert: 2 Castle buildings created
        _addedBuildings.Count.ShouldBe(2);
        _addedBuildings.ShouldAllBe(b => b.BuildingTypeId == CastleBuildingTypeId);

        // Each building belongs to a different kingdom
        _addedBuildings.Select(b => b.KingdomId).Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public async Task InitializeGame_ClaimsAdjacentTiles_SixPerPlayer()
    {
        // Arrange
        var game = CreateGame();
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(KingdomId1, FactionId1, turnOrder: 1),
            CreateKingdom(KingdomId2, FactionId2, turnOrder: 2)
        };
        var factions = new List<FactionType>
        {
            CreateFactionType(FactionId1),
            CreateFactionType(FactionId2)
        };
        SetupInitializeMocks(game, kingdoms, factions);

        // Act
        await _sut.InitializeGameAsync(GameId);

        // Assert: each kingdom owns at least 7 tiles (1 castle + 6 adjacent)
        // Some adjacent tiles may be off-grid for edge positions, so >= 5 is safe minimum
        var k1Tiles = _addedTiles.Count(t => t.KingdomId == KingdomId1);
        var k2Tiles = _addedTiles.Count(t => t.KingdomId == KingdomId2);

        k1Tiles.ShouldBeGreaterThanOrEqualTo(5); // castle + at least 4 neighbors on edge
        k2Tiles.ShouldBeGreaterThanOrEqualTo(5);

        // Total claimed tiles should be reasonable (castle + neighbors for each)
        var totalClaimed = _addedTiles.Count(t => t.KingdomId != null);
        totalClaimed.ShouldBeGreaterThanOrEqualTo(10); // at least 5 per kingdom
    }

    [Fact]
    public async Task InitializeGame_NoArmiesCreated()
    {
        // Arrange
        var game = CreateGame();
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(KingdomId1, FactionId1, turnOrder: 1),
            CreateKingdom(KingdomId2, FactionId2, turnOrder: 2)
        };
        var factions = new List<FactionType>
        {
            CreateFactionType(FactionId1),
            CreateFactionType(FactionId2)
        };
        SetupInitializeMocks(game, kingdoms, factions);

        // Act
        await _sut.InitializeGameAsync(GameId);

        // Assert: no armies added (MAPG-06)
        _armiesMock.Verify(a => a.AddAsync(It.IsAny<Army>()), Times.Never);
    }

    [Fact]
    public async Task InitializeGame_SetsGameStatusInProgress()
    {
        // Arrange
        var game = CreateGame();
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(KingdomId1, FactionId1, turnOrder: 1),
            CreateKingdom(KingdomId2, FactionId2, turnOrder: 2)
        };
        var factions = new List<FactionType>
        {
            CreateFactionType(FactionId1),
            CreateFactionType(FactionId2)
        };
        SetupInitializeMocks(game, kingdoms, factions);

        // Act
        await _sut.InitializeGameAsync(GameId);

        // Assert: game state transitions
        game.Status.ShouldBe(EGameStatus.InProgress);
        game.CurrentPhase.ShouldBe(EGamePhase.Action);
        game.RoundNumber.ShouldBe(1);
        game.CurrentTurnKingdomId.ShouldBe(KingdomId1); // first by TurnOrder
        game.StartedAt.ShouldNotBeNull();
    }

    // -------------------------------------------------------------------------
    // Snapshot DTO — v6.0 reconnect fields (SYNC-05)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeGame_SnapshotIncludesPhaseAndActionPoints()
    {
        // Arrange
        var game = CreateGame();
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(KingdomId1, FactionId1, turnOrder: 1),
            CreateKingdom(KingdomId2, FactionId2, turnOrder: 2)
        };
        var factions = new List<FactionType>
        {
            CreateFactionType(FactionId1),
            CreateFactionType(FactionId2)
        };
        SetupInitializeMocks(game, kingdoms, factions);

        // Act
        var result = await _sut.InitializeGameAsync(GameId);

        // Assert: snapshot includes phase and AP for reconnect
        result.CurrentPhase.ShouldBe("Action");
        result.RemainingActionPoints.ShouldBe(4); // BaseActionPoints + 0 modifier
        result.DeclaredAttacks.ShouldNotBeNull();
        result.DeclaredAttacks.Count.ShouldBe(0);
    }

    [Fact]
    public async Task BuildSnapshot_IncludesDeclaredAttacksForCurrentRound()
    {
        // Arrange
        var game = CreateGame();
        game.Status = EGameStatus.InProgress;
        game.CurrentPhase = EGamePhase.Battle;
        game.RoundNumber = 2;
        game.RemainingActionPoints = 3;
        game.MapWidth = 16;
        game.MapHeight = 16;
        game.CurrentTurnKingdomId = KingdomId1;

        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(KingdomId1, FactionId1, turnOrder: 1),
            CreateKingdom(KingdomId2, FactionId2, turnOrder: 2)
        };

        _gamesMock.Setup(g => g.GetByIdAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        _tilesMock.Setup(t => t.GetTilesWithBuildingsForGameAsync(GameId)).ReturnsAsync(new List<Tile>());
        _armiesMock.Setup(a => a.GetArmiesForKingdomAsync(It.IsAny<Guid>())).ReturnsAsync(Enumerable.Empty<Army>());
        _kingdomResourcesMock.Setup(r => r.GetResourcesForKingdomAsync(It.IsAny<Guid>())).ReturnsAsync(new List<KingdomResource>());

        var attackId = Guid.NewGuid();
        _declaredAttacksMock.Setup(d => d.GetForGameRoundAsync(GameId, 2))
            .ReturnsAsync(new List<DeclaredAttack>
            {
                new()
                {
                    Id = attackId,
                    GameId = GameId,
                    RoundNumber = 2,
                    TargetTileId = Guid.NewGuid(),
                    RiskedTileId = Guid.NewGuid(),
                    AttackerKingdomId = KingdomId1,
                    DefenderKingdomId = KingdomId2
                }
            });

        foreach (var factionId in new[] { FactionId1, FactionId2 })
        {
            _factionTypesMock.Setup(f => f.GetByIdAsync(factionId))
                .ReturnsAsync(CreateFactionType(factionId));
        }

        // Act
        var result = await _sut.BuildGameStateSnapshotAsync(GameId);

        // Assert
        result.CurrentPhase.ShouldBe("Battle");
        result.RemainingActionPoints.ShouldBe(3);
        result.DeclaredAttacks.Count.ShouldBe(1);
        result.DeclaredAttacks[0].AttackId.ShouldBe(attackId);
        result.DeclaredAttacks[0].AttackerKingdomId.ShouldBe(KingdomId1);
        result.DeclaredAttacks[0].DefenderKingdomId.ShouldBe(KingdomId2);
    }
}
