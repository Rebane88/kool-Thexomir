using Application.Contracts;
using Application.Services.GameInitialization;
using Base;
using Domain.Buildings;
using Domain.Factions;
using Domain.Game;
using Domain.Map;
using Domain.Resources;
using Moq;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// GameInitializationService unit tests -- fully mocked, no database dependency.
/// Verifies hex map generation, kingdom territory placement, capital buildings,
/// resource seeding, and game metadata updates.
/// </summary>
public class GameInitializationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGameRepository> _gamesMock = new();
    private readonly Mock<IKingdomRepository> _kingdomsMock = new();
    private readonly Mock<ITerrainTypeRepository> _terrainTypesMock = new();
    private readonly Mock<ITileRepository> _tilesMock = new();
    private readonly Mock<IBuildingRepository> _buildingsMock = new();
    private readonly Mock<IKingdomResourceRepository> _kingdomResourcesMock = new();
    private readonly Mock<IFactionTypeRepository> _factionTypesMock = new();
    private readonly GameInitializationService _sut;

    // Captured entities for assertions
    private readonly List<Tile> _addedTiles = [];
    private readonly List<Building> _addedBuildings = [];
    private readonly List<KingdomResource> _addedResources = [];

    // Well-known IDs
    private static readonly Guid GameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid KingdomId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid KingdomId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserId1 = Guid.Parse("eeeeeeee-1111-1111-1111-111111111111");
    private static readonly Guid UserId2 = Guid.Parse("eeeeeeee-2222-2222-2222-222222222222");
    private static readonly Guid FactionId1 = Guid.Parse("eeeeeeee-0001-0000-0000-000000000001");
    private static readonly Guid FactionId2 = Guid.Parse("eeeeeeee-0001-0000-0000-000000000002");
    private static readonly Guid PlainsTerrainId = Guid.Parse("AAAAAAAA-0001-0000-0000-000000000001");
    private static readonly Guid CapitalBuildingTypeId = Guid.Parse("BBBBBBBB-0001-0000-0000-000000000100");

    public GameInitializationServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Games).Returns(_gamesMock.Object);
        _unitOfWorkMock.Setup(u => u.Kingdoms).Returns(_kingdomsMock.Object);
        _unitOfWorkMock.Setup(u => u.TerrainTypes).Returns(_terrainTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.Tiles).Returns(_tilesMock.Object);
        _unitOfWorkMock.Setup(u => u.Buildings).Returns(_buildingsMock.Object);
        _unitOfWorkMock.Setup(u => u.KingdomResources).Returns(_kingdomResourcesMock.Object);
        _unitOfWorkMock.Setup(u => u.FactionTypes).Returns(_factionTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        // Capture added entities
        _tilesMock.Setup(t => t.AddAsync(It.IsAny<Tile>()))
            .ReturnsAsync((Tile t) => { _addedTiles.Add(t); return t; });
        _buildingsMock.Setup(b => b.AddAsync(It.IsAny<Building>()))
            .ReturnsAsync((Building b) => { _addedBuildings.Add(b); return b; });
        _kingdomResourcesMock.Setup(r => r.AddAsync(It.IsAny<KingdomResource>()))
            .ReturnsAsync((KingdomResource r) => { _addedResources.Add(r); return r; });

        _sut = new GameInitializationService(_unitOfWorkMock.Object);
    }

    private void SetupStandardGame()
    {
        var game = new Game
        {
            Id = GameId,
            Status = EGameStatus.InProgress,
            MaxPlayers = 4,
            WinCondition = EWinCondition.Domination,
            LobbyCode = "ABCDEF",
            HostUserId = UserId1,
        };

        var kingdoms = new List<Kingdom>
        {
            new()
            {
                Id = KingdomId1, GameId = GameId, AppUserId = UserId1,
                FactionTypeId = FactionId1, Name = "Kingdom 1"
            },
            new()
            {
                Id = KingdomId2, GameId = GameId, AppUserId = UserId2,
                FactionTypeId = FactionId2, Name = "Kingdom 2"
            },
        };

        var terrainTypes = new List<TerrainType>
        {
            new() { Id = Guid.Parse("AAAAAAAA-0001-0000-0000-000000000001"), Name = new LangStr("Plains", "en") },
            new() { Id = Guid.Parse("AAAAAAAA-0001-0000-0000-000000000002"), Name = new LangStr("Forest", "en") },
            new() { Id = Guid.Parse("AAAAAAAA-0001-0000-0000-000000000003"), Name = new LangStr("Mountain", "en") },
            new() { Id = Guid.Parse("AAAAAAAA-0001-0000-0000-000000000004"), Name = new LangStr("River", "en") },
            new() { Id = Guid.Parse("AAAAAAAA-0001-0000-0000-000000000005"), Name = new LangStr("Magic Grove", "en") },
        };

        var faction1 = new FactionType
        {
            Id = FactionId1, Name = new LangStr("Iron Throne", "en"),
            StartingGold = 50, StartingFood = 0, StartingWood = 0, StartingStone = 0, StartingMana = 0,
        };
        var faction2 = new FactionType
        {
            Id = FactionId2, Name = new LangStr("Mage Council", "en"),
            StartingGold = 0, StartingFood = 0, StartingWood = 0, StartingStone = 0, StartingMana = 30,
        };

        _gamesMock.Setup(g => g.GetByIdForUpdateAsync(GameId)).ReturnsAsync(game);
        _gamesMock.Setup(g => g.GetByIdAsync(GameId)).ReturnsAsync(game);
        _gamesMock.Setup(g => g.UpdateAsync(It.IsAny<Game>())).ReturnsAsync((Game g) => g);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        _terrainTypesMock.Setup(t => t.GetAllAsync()).ReturnsAsync(terrainTypes);
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId1)).ReturnsAsync(faction1);
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId2)).ReturnsAsync(faction2);
    }

    // -------------------------------------------------------------------------
    // Test 1: Tile count matches expected hex grid size
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeGameAsync_TwoPlayers_Creates61Tiles()
    {
        SetupStandardGame();

        await _sut.InitializeGameAsync(GameId);

        // 2 players -> radius 4 -> hex grid has 3*4^2 + 3*4 + 1 = 48+12+1 = 61 tiles
        _addedTiles.Count.ShouldBe(61);
    }

    // -------------------------------------------------------------------------
    // Test 2: Each kingdom gets owned tiles (center + neighbors)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeGameAsync_EachKingdomGets7OwnedTiles()
    {
        SetupStandardGame();

        await _sut.InitializeGameAsync(GameId);

        var kingdom1Tiles = _addedTiles.Where(t => t.KingdomId == KingdomId1).ToList();
        var kingdom2Tiles = _addedTiles.Where(t => t.KingdomId == KingdomId2).ToList();

        // Each kingdom should get up to 7 tiles (center + 6 neighbors)
        // Some edge tiles might be outside the grid, so at least 4 but up to 7
        kingdom1Tiles.Count.ShouldBeInRange(4, 7);
        kingdom2Tiles.Count.ShouldBeInRange(4, 7);
    }

    // -------------------------------------------------------------------------
    // Test 3: Each kingdom gets a Capital building
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeGameAsync_EachKingdomGetsCapitalBuilding()
    {
        SetupStandardGame();

        await _sut.InitializeGameAsync(GameId);

        _addedBuildings.Count.ShouldBe(2); // one per kingdom
        _addedBuildings.ShouldAllBe(b => b.BuildingTypeId == CapitalBuildingTypeId);
    }

    // -------------------------------------------------------------------------
    // Test 4: Each kingdom gets 5 resource rows matching faction values
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeGameAsync_SeedsResourcesFromFactionStartingValues()
    {
        SetupStandardGame();

        await _sut.InitializeGameAsync(GameId);

        // 2 kingdoms * 5 resource types = 10 resource rows
        _addedResources.Count.ShouldBe(10);

        // Kingdom 1 (Iron Throne): 50 gold, 0 of rest
        var k1Resources = _addedResources.Where(r => r.KingdomId == KingdomId1).ToList();
        k1Resources.Count.ShouldBe(5);
        k1Resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(50);
        k1Resources.Single(r => r.ResourceType == EResourceType.Mana).Amount.ShouldBe(0);

        // Kingdom 2 (Mage Council): 30 mana, 0 of rest
        var k2Resources = _addedResources.Where(r => r.KingdomId == KingdomId2).ToList();
        k2Resources.Count.ShouldBe(5);
        k2Resources.Single(r => r.ResourceType == EResourceType.Mana).Amount.ShouldBe(30);
        k2Resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(0);
    }

    // -------------------------------------------------------------------------
    // Test 5: Game metadata is updated
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeGameAsync_SetsGameTurnAndMapWidth()
    {
        SetupStandardGame();

        var result = await _sut.InitializeGameAsync(GameId);

        result.TurnNumber.ShouldBe(1);
        result.MapRadius.ShouldBe(4); // radius = playerCount + 2 = 2 + 2 = 4
    }

    // -------------------------------------------------------------------------
    // Test 6: Returned DTO contains all data
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeGameAsync_ReturnsDtoWithAllTilesAndKingdoms()
    {
        SetupStandardGame();

        var result = await _sut.InitializeGameAsync(GameId);

        result.GameId.ShouldBe(GameId);
        result.Tiles.Count.ShouldBe(61);
        result.Kingdoms.Count.ShouldBe(2);
        result.Status.ShouldBe(EGameStatus.InProgress.ToString());
        result.WinCondition.ShouldBe(EWinCondition.Domination.ToString());

        // Each kingdom should have 5 resources in DTO
        result.Kingdoms.ShouldAllBe(k => k.Resources.Count == 5);
    }
}
