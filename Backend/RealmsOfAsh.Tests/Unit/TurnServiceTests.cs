using Application.Contracts;
using Application.Services.Turn;
using Application.Services.Turn.DTOs;
using Base.Contracts;
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
/// TurnService unit tests -- fully mocked, no database dependency.
/// Verifies round-robin turn advancement, income calculation with
/// terrain and faction modifiers, and turn logging.
/// </summary>
public class TurnServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGameGuard> _gameGuardMock = new();
    private readonly Mock<IGameRepository> _gamesMock = new();
    private readonly Mock<IKingdomRepository> _kingdomsMock = new();
    private readonly Mock<ITileRepository> _tilesMock = new();
    private readonly Mock<IKingdomResourceRepository> _kingdomResourcesMock = new();
    private readonly Mock<IFactionResourceBonusRepository> _factionResourceBonusesMock = new();
    private readonly Mock<ITurnLogRepository> _turnLogsMock = new();
    private readonly Mock<IArmyRepository> _armiesMock = new();
    private readonly Mock<IBuildingRepository> _buildingsMock = new();
    private readonly TurnService _sut;

    // Captured entities for assertions
    private readonly List<TurnLog> _addedTurnLogs = [];

    // Well-known IDs
    private static readonly Guid GameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Kingdom1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Kingdom2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Kingdom3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid UserId = Guid.Parse("eeeeeeee-1111-1111-1111-111111111111");
    private static readonly Guid FactionId = Guid.Parse("eeeeeeee-0001-0000-0000-000000000001");

    public TurnServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Games).Returns(_gamesMock.Object);
        _unitOfWorkMock.Setup(u => u.Kingdoms).Returns(_kingdomsMock.Object);
        _unitOfWorkMock.Setup(u => u.Tiles).Returns(_tilesMock.Object);
        _unitOfWorkMock.Setup(u => u.KingdomResources).Returns(_kingdomResourcesMock.Object);
        _unitOfWorkMock.Setup(u => u.FactionResourceBonuses).Returns(_factionResourceBonusesMock.Object);
        _unitOfWorkMock.Setup(u => u.TurnLogs).Returns(_turnLogsMock.Object);
        _unitOfWorkMock.Setup(u => u.Armies).Returns(_armiesMock.Object);
        _unitOfWorkMock.Setup(u => u.Buildings).Returns(_buildingsMock.Object);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        // Per-turn flag reset: return empty collections by default
        _armiesMock.Setup(a => a.GetArmiesForKingdomAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Enumerable.Empty<Army>());
        _buildingsMock.Setup(b => b.GetBuildingsForKingdomAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<Building>());

        _turnLogsMock.Setup(t => t.AddAsync(It.IsAny<TurnLog>()))
            .ReturnsAsync((TurnLog tl) => { _addedTurnLogs.Add(tl); return tl; });

        _gamesMock.Setup(g => g.UpdateAsync(It.IsAny<Game>())).ReturnsAsync((Game g) => g);
        _kingdomResourcesMock.Setup(r => r.UpdateAsync(It.IsAny<KingdomResource>()))
            .ReturnsAsync((KingdomResource r) => r);

        _sut = new TurnService(_unitOfWorkMock.Object, _gameGuardMock.Object);
    }

    private Game CreateGame(Guid currentTurnKingdomId, int turnNumber = 1) => new()
    {
        Id = GameId,
        Status = EGameStatus.InProgress,
        TurnNumber = turnNumber,
        CurrentTurnKingdomId = currentTurnKingdomId,
    };

    private List<Kingdom> CreateThreeKingdoms(Guid? eliminatedId = null) =>
    [
        new()
        {
            Id = Kingdom1Id, GameId = GameId, FactionTypeId = FactionId,
            IsEliminated = eliminatedId == Kingdom1Id,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        },
        new()
        {
            Id = Kingdom2Id, GameId = GameId, FactionTypeId = FactionId,
            IsEliminated = eliminatedId == Kingdom2Id,
            CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
        },
        new()
        {
            Id = Kingdom3Id, GameId = GameId, FactionTypeId = FactionId,
            IsEliminated = eliminatedId == Kingdom3Id,
            CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
        },
    ];

    private void SetupGuardSuccess(Game game, Kingdom kingdom)
    {
        _gameGuardMock.Setup(g => g.ValidateAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Ok(new GameGuardContext(game, kingdom)));
    }

    private void SetupEmptyIncome(Guid kingdomId)
    {
        _tilesMock.Setup(t => t.GetTilesWithBuildingsAndTerrainForKingdomAsync(kingdomId))
            .ReturnsAsync([]);
        _factionResourceBonusesMock.Setup(f => f.GetBonusesForFactionAsync(FactionId))
            .ReturnsAsync([]);
        _kingdomResourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(kingdomId))
            .ReturnsAsync(CreateDefaultResources(kingdomId));
    }

    private static List<KingdomResource> CreateDefaultResources(Guid kingdomId, decimal initialGold = 0) =>
    [
        new() { KingdomId = kingdomId, ResourceType = EResourceType.Gold, Amount = initialGold },
        new() { KingdomId = kingdomId, ResourceType = EResourceType.Food, Amount = 0 },
        new() { KingdomId = kingdomId, ResourceType = EResourceType.Wood, Amount = 0 },
        new() { KingdomId = kingdomId, ResourceType = EResourceType.Stone, Amount = 0 },
        new() { KingdomId = kingdomId, ResourceType = EResourceType.Mana, Amount = 0 },
    ];

    // -------------------------------------------------------------------------
    // Test 1: Guard failure propagation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurn_GuardFails_ReturnsFailure()
    {
        _gameGuardMock.Setup(g => g.ValidateAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Fail("It is not your turn."));

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("It is not your turn.");
    }

    // -------------------------------------------------------------------------
    // Test 2: Advances to next kingdom in order
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurn_AdvancesToNextKingdom()
    {
        var kingdoms = CreateThreeKingdoms();
        var game = CreateGame(Kingdom1Id);
        SetupGuardSuccess(game, kingdoms[0]);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        SetupEmptyIncome(Kingdom2Id);

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.NewKingdomId.ShouldBe(Kingdom2Id);
        result.Value.TurnNumber.ShouldBe(1); // no wrap, turn stays 1
        game.CurrentTurnKingdomId.ShouldBe(Kingdom2Id);
    }

    // -------------------------------------------------------------------------
    // Test 3: Skips eliminated kingdoms
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurn_SkipsEliminatedKingdoms()
    {
        var kingdoms = CreateThreeKingdoms(eliminatedId: Kingdom2Id);
        var game = CreateGame(Kingdom1Id);
        SetupGuardSuccess(game, kingdoms[0]);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        SetupEmptyIncome(Kingdom3Id);

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.NewKingdomId.ShouldBe(Kingdom3Id);
    }

    // -------------------------------------------------------------------------
    // Test 4: Wraps around to first player and increments TurnNumber
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurn_WrapsAroundToFirst()
    {
        var kingdoms = CreateThreeKingdoms();
        var game = CreateGame(Kingdom3Id, turnNumber: 1);
        SetupGuardSuccess(game, kingdoms[2]);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        SetupEmptyIncome(Kingdom1Id);

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.NewKingdomId.ShouldBe(Kingdom1Id);
        result.Value.TurnNumber.ShouldBe(2); // wrapped, turn increments
        game.TurnNumber.ShouldBe(2);
    }

    // -------------------------------------------------------------------------
    // Test 5: Applies income to next player
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurn_AppliesIncomeToNextPlayer()
    {
        var kingdoms = CreateThreeKingdoms();
        var game = CreateGame(Kingdom1Id);
        SetupGuardSuccess(game, kingdoms[0]);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);

        // Kingdom2 has a building with GoldYield=5 on Plains (Food bonus, not Gold)
        var tiles = new List<Tile>
        {
            new()
            {
                Id = Guid.NewGuid(),
                KingdomId = Kingdom2Id,
                TerrainType = new TerrainType
                {
                    ResourceBonusType = ETerrainResourceBonus.Food, // not gold
                },
                Buildings =
                [
                    new Building
                    {
                        BuildingType = new BuildingType { GoldYield = 5 },
                    },
                ],
            },
        };
        _tilesMock.Setup(t => t.GetTilesWithBuildingsAndTerrainForKingdomAsync(Kingdom2Id))
            .ReturnsAsync(tiles);
        _factionResourceBonusesMock.Setup(f => f.GetBonusesForFactionAsync(FactionId))
            .ReturnsAsync([new FactionResourceBonus { ResourceType = EResourceType.Gold, Multiplier = 1.0m }]);

        var resources = CreateDefaultResources(Kingdom2Id, initialGold: 10);
        _kingdomResourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(Kingdom2Id))
            .ReturnsAsync(resources);

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        // floor(5 * 1.0 * 1.0) = 5 Gold added
        resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(15);
        result.Value!.IncomeApplied["Gold"].ShouldBe(5);
    }

    // -------------------------------------------------------------------------
    // Test 6: Terrain bonus applied (1.25x)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurn_TerrainBonusApplied()
    {
        var kingdoms = CreateThreeKingdoms();
        var game = CreateGame(Kingdom1Id);
        SetupGuardSuccess(game, kingdoms[0]);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);

        // Building with FoodYield=10 on Plains (Food bonus = match)
        var tiles = new List<Tile>
        {
            new()
            {
                Id = Guid.NewGuid(),
                KingdomId = Kingdom2Id,
                TerrainType = new TerrainType
                {
                    ResourceBonusType = ETerrainResourceBonus.Food, // matches Food
                },
                Buildings =
                [
                    new Building
                    {
                        BuildingType = new BuildingType { FoodYield = 10 },
                    },
                ],
            },
        };
        _tilesMock.Setup(t => t.GetTilesWithBuildingsAndTerrainForKingdomAsync(Kingdom2Id))
            .ReturnsAsync(tiles);
        _factionResourceBonusesMock.Setup(f => f.GetBonusesForFactionAsync(FactionId))
            .ReturnsAsync([]); // no faction bonus = 1.0

        var resources = CreateDefaultResources(Kingdom2Id);
        _kingdomResourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(Kingdom2Id))
            .ReturnsAsync(resources);

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        // floor(10 * 1.25 * 1.0) = 12 Food
        resources.Single(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(12);
        result.Value!.IncomeApplied["Food"].ShouldBe(12);
    }

    // -------------------------------------------------------------------------
    // Test 7: Faction multiplier applied
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurn_FactionMultiplierApplied()
    {
        var kingdoms = CreateThreeKingdoms();
        var game = CreateGame(Kingdom1Id);
        SetupGuardSuccess(game, kingdoms[0]);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);

        // Building with GoldYield=5, no terrain match, faction multiplier 1.3 for Gold
        var tiles = new List<Tile>
        {
            new()
            {
                Id = Guid.NewGuid(),
                KingdomId = Kingdom2Id,
                TerrainType = new TerrainType
                {
                    ResourceBonusType = ETerrainResourceBonus.None, // no match
                },
                Buildings =
                [
                    new Building
                    {
                        BuildingType = new BuildingType { GoldYield = 5 },
                    },
                ],
            },
        };
        _tilesMock.Setup(t => t.GetTilesWithBuildingsAndTerrainForKingdomAsync(Kingdom2Id))
            .ReturnsAsync(tiles);
        _factionResourceBonusesMock.Setup(f => f.GetBonusesForFactionAsync(FactionId))
            .ReturnsAsync([new FactionResourceBonus { ResourceType = EResourceType.Gold, Multiplier = 1.3m }]);

        var resources = CreateDefaultResources(Kingdom2Id);
        _kingdomResourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(Kingdom2Id))
            .ReturnsAsync(resources);

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        // floor(5 * 1.0 * 1.3) = floor(6.5) = 6 Gold
        resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(6);
        result.Value!.IncomeApplied["Gold"].ShouldBe(6);
    }

    // -------------------------------------------------------------------------
    // Test 8: Floor per building
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurn_FloorPerBuilding()
    {
        var kingdoms = CreateThreeKingdoms();
        var game = CreateGame(Kingdom1Id);
        SetupGuardSuccess(game, kingdoms[0]);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);

        // Building with WoodYield=3, terrain match (Wood, 1.25x), faction multiplier 1.1
        var tiles = new List<Tile>
        {
            new()
            {
                Id = Guid.NewGuid(),
                KingdomId = Kingdom2Id,
                TerrainType = new TerrainType
                {
                    ResourceBonusType = ETerrainResourceBonus.Wood, // matches Wood
                },
                Buildings =
                [
                    new Building
                    {
                        BuildingType = new BuildingType { WoodYield = 3 },
                    },
                ],
            },
        };
        _tilesMock.Setup(t => t.GetTilesWithBuildingsAndTerrainForKingdomAsync(Kingdom2Id))
            .ReturnsAsync(tiles);
        _factionResourceBonusesMock.Setup(f => f.GetBonusesForFactionAsync(FactionId))
            .ReturnsAsync([new FactionResourceBonus { ResourceType = EResourceType.Wood, Multiplier = 1.1m }]);

        var resources = CreateDefaultResources(Kingdom2Id);
        _kingdomResourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(Kingdom2Id))
            .ReturnsAsync(resources);

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        // floor(3 * 1.25 * 1.1) = floor(4.125) = 4 Wood
        resources.Single(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(4);
        result.Value!.IncomeApplied["Wood"].ShouldBe(4);
    }

    // -------------------------------------------------------------------------
    // Test 9: Logs turn action
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurn_LogsTurnAction()
    {
        var kingdoms = CreateThreeKingdoms();
        var game = CreateGame(Kingdom1Id, turnNumber: 3);
        SetupGuardSuccess(game, kingdoms[0]);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        SetupEmptyIncome(Kingdom2Id);

        await _sut.EndTurnAsync(GameId, UserId);

        _addedTurnLogs.Count.ShouldBe(1);
        _addedTurnLogs[0].Action.ShouldBe("EndTurn");
        _addedTurnLogs[0].TurnNumber.ShouldBe(3);
        _addedTurnLogs[0].KingdomId.ShouldBe(Kingdom1Id);
        _addedTurnLogs[0].GameId.ShouldBe(GameId);
    }
}
