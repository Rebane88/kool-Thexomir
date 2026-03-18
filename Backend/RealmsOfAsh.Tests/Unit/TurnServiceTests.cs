using Application.Contracts;
using Application.Services.Turn;
using Application.Services.Turn.DTOs;
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
public class TurnServiceTests
{
    // Mocks
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGameGuard> _gameGuardMock = new();
    private readonly Mock<IKingdomRepository> _kingdomsMock = new();
    private readonly Mock<ITurnLogRepository> _turnLogsMock = new();
    private readonly Mock<IFactionTypeRepository> _factionTypesMock = new();
    private readonly Mock<ITileRepository> _tilesMock = new();
    private readonly Mock<IBuildingRepository> _buildingsMock = new();
    private readonly Mock<IKingdomResourceRepository> _resourcesMock = new();
    private readonly Mock<IBuildingTypeRepository> _buildingTypesMock = new();
    private readonly TurnService _sut;

    // Fixed IDs
    private static readonly Guid GameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Kingdom1Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Kingdom2Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid Kingdom3Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid FactionTypeId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    public TurnServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Kingdoms).Returns(_kingdomsMock.Object);
        _unitOfWorkMock.Setup(u => u.TurnLogs).Returns(_turnLogsMock.Object);
        _unitOfWorkMock.Setup(u => u.FactionTypes).Returns(_factionTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.Tiles).Returns(_tilesMock.Object);
        _unitOfWorkMock.Setup(u => u.Buildings).Returns(_buildingsMock.Object);
        _unitOfWorkMock.Setup(u => u.KingdomResources).Returns(_resourcesMock.Object);
        _unitOfWorkMock.Setup(u => u.BuildingTypes).Returns(_buildingTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        _sut = new TurnService(_unitOfWorkMock.Object, _gameGuardMock.Object);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Game CreateGame(int roundNumber = 1) => new()
    {
        Id = GameId,
        Status = EGameStatus.InProgress,
        RoundNumber = roundNumber,
        CurrentPhase = EGamePhase.Action,
        CurrentTurnKingdomId = Kingdom1Id,
        BaseActionPoints = 4,
        MaxRounds = 100,
        RemainingActionPoints = 4
    };

    private static Kingdom CreateKingdom(Guid id, int turnOrder, EKingdomStatus status = EKingdomStatus.Active) => new()
    {
        Id = id,
        GameId = GameId,
        AppUserId = id == Kingdom1Id ? UserId : Guid.NewGuid(),
        FactionTypeId = FactionTypeId,
        Status = status,
        TurnOrder = turnOrder,
        Name = $"Kingdom {turnOrder}"
    };

    private static FactionType CreateFactionType(int apModifier = 0) => new()
    {
        Id = FactionTypeId,
        Name = new LangStr("Test Faction", "en"),
        ActionPointModifier = apModifier,
        ResourceProductionModifier = 1.0m
    };

    private void SetupGuardSuccess(Game? game = null, Kingdom? kingdom = null)
    {
        var g = game ?? CreateGame();
        var k = kingdom ?? CreateKingdom(Kingdom1Id, 1);
        _gameGuardMock.Setup(gg => gg.ValidateAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Ok(new GameGuardContext(g, k)));
    }

    private void SetupKingdoms(params Kingdom[] kingdoms)
    {
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId))
            .ReturnsAsync(kingdoms.ToList());
    }

    private void SetupFactionType(FactionType? ft = null)
    {
        var factionType = ft ?? CreateFactionType();
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionTypeId)).ReturnsAsync(factionType);
    }

    private void SetupIncomeForPhaseTransition()
    {
        // No buildings -- income returns empty
        _tilesMock.Setup(t => t.GetTilesWithBuildingsAndTerrainForKingdomAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<Tile>());
    }

    // -------------------------------------------------------------------------
    // Guard failure tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurnAsync_GuardFailure_ReturnsFailResult()
    {
        _gameGuardMock.Setup(gg => gg.ValidateAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Fail("Game not found."));

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Game not found.");
    }

    // -------------------------------------------------------------------------
    // Validation failure tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurnAsync_WrongPhase_ReturnsFailResult()
    {
        var game = CreateGame();
        game.CurrentPhase = EGamePhase.Income;
        SetupGuardSuccess(game);

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Can only end turn during Action Phase.");
    }

    [Fact]
    public async Task EndTurnAsync_WrongKingdom_ReturnsFailResult()
    {
        var game = CreateGame();
        game.CurrentTurnKingdomId = Kingdom2Id;
        SetupGuardSuccess(game);

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("It is not your turn.");
    }

    // -------------------------------------------------------------------------
    // Success: next player
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurnAsync_SuccessWithNextPlayer_AdvancesToNextKingdom()
    {
        var game = CreateGame();
        SetupGuardSuccess(game);
        var k1 = CreateKingdom(Kingdom1Id, 1);
        var k2 = CreateKingdom(Kingdom2Id, 2);
        SetupKingdoms(k1, k2);
        SetupFactionType();

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.NextKingdomId.ShouldBe(Kingdom2Id);
        result.Value.PhaseChanged.ShouldBeFalse();
        result.Value.ActionPoints.ShouldBe(4);
        game.CurrentTurnKingdomId.ShouldBe(Kingdom2Id);
        game.RemainingActionPoints.ShouldBe(4);
    }

    [Fact]
    public async Task EndTurnAsync_SuccessWithNextPlayer_CreatesTurnLogs()
    {
        var game = CreateGame();
        SetupGuardSuccess(game);
        var k1 = CreateKingdom(Kingdom1Id, 1);
        var k2 = CreateKingdom(Kingdom2Id, 2);
        SetupKingdoms(k1, k2);
        SetupFactionType();

        await _sut.EndTurnAsync(GameId, UserId);

        _turnLogsMock.Verify(t => t.AddAsync(It.Is<TurnLog>(
            tl => tl.EventType == EEventType.TurnEnded)), Times.Once);
        _turnLogsMock.Verify(t => t.AddAsync(It.Is<TurnLog>(
            tl => tl.EventType == EEventType.TurnStarted)), Times.Once);
        _turnLogsMock.Verify(t => t.AddAsync(It.Is<TurnLog>(
            tl => tl.EventType == EEventType.ActionPointsReceived)), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(default), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Success: skip defeated kingdom
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurnAsync_SkipsDefeatedKingdom_AdvancesToNextActive()
    {
        var game = CreateGame();
        SetupGuardSuccess(game);
        var k1 = CreateKingdom(Kingdom1Id, 1);
        var k2 = CreateKingdom(Kingdom2Id, 2, EKingdomStatus.Defeated);
        var k3 = CreateKingdom(Kingdom3Id, 3);
        SetupKingdoms(k1, k2, k3);
        SetupFactionType();

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.NextKingdomId.ShouldBe(Kingdom3Id);
        game.CurrentTurnKingdomId.ShouldBe(Kingdom3Id);
    }

    // -------------------------------------------------------------------------
    // Success: last player -> phase transition
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurnAsync_LastPlayer_TransitionsThroughPhases()
    {
        var game = CreateGame();
        SetupGuardSuccess(game);
        var k1 = CreateKingdom(Kingdom1Id, 1);
        SetupKingdoms(k1); // only one player -- ending turn triggers phase transition
        SetupFactionType();
        SetupIncomeForPhaseTransition();

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.PhaseChanged.ShouldBeTrue();
        result.Value.CurrentPhase.ShouldBe("Action");
        result.Value.RoundNumber.ShouldBe(2); // incremented
        result.Value.NextKingdomId.ShouldBe(Kingdom1Id); // wraps back to first
        game.RoundNumber.ShouldBe(2);
        game.CurrentPhase.ShouldBe(EGamePhase.Action);
    }

    [Fact]
    public async Task EndTurnAsync_LastPlayer_CreatesPhaseChangedTurnLogs()
    {
        var game = CreateGame();
        SetupGuardSuccess(game);
        var k1 = CreateKingdom(Kingdom1Id, 1);
        SetupKingdoms(k1);
        SetupFactionType();
        SetupIncomeForPhaseTransition();

        await _sut.EndTurnAsync(GameId, UserId);

        // Should have PhaseChanged logs for Battle, Income, RoundEnd, Action
        _turnLogsMock.Verify(t => t.AddAsync(It.Is<TurnLog>(
            tl => tl.EventType == EEventType.PhaseChanged)), Times.Exactly(4));
        _turnLogsMock.Verify(t => t.AddAsync(It.Is<TurnLog>(
            tl => tl.EventType == EEventType.RoundEnded)), Times.Once);
        _turnLogsMock.Verify(t => t.AddAsync(It.Is<TurnLog>(
            tl => tl.EventType == EEventType.RoundStarted)), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Turn deadline tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurnAsync_WithTurnTimeLimit_SetsTurnDeadline()
    {
        var game = CreateGame();
        game.TurnTimeLimit = 60;
        SetupGuardSuccess(game);
        var k1 = CreateKingdom(Kingdom1Id, 1);
        var k2 = CreateKingdom(Kingdom2Id, 2);
        SetupKingdoms(k1, k2);
        SetupFactionType();

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.TurnDeadline.ShouldNotBeNull();
        game.TurnDeadline.ShouldNotBeNull();
    }

    [Fact]
    public async Task EndTurnAsync_WithoutTurnTimeLimit_TurnDeadlineIsNull()
    {
        var game = CreateGame();
        game.TurnTimeLimit = null;
        SetupGuardSuccess(game);
        var k1 = CreateKingdom(Kingdom1Id, 1);
        var k2 = CreateKingdom(Kingdom2Id, 2);
        SetupKingdoms(k1, k2);
        SetupFactionType();

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.TurnDeadline.ShouldBeNull();
        game.TurnDeadline.ShouldBeNull();
    }

    // -------------------------------------------------------------------------
    // Income application tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurnAsync_LastPlayer_AppliesIncome()
    {
        var game = CreateGame();
        SetupGuardSuccess(game);
        var k1 = CreateKingdom(Kingdom1Id, 1);
        SetupKingdoms(k1);
        SetupFactionType();

        // Setup tiles with buildings and terrain for income calculation
        var terrainType = new TerrainType
        {
            Id = Guid.NewGuid(),
            Name = new LangStr("Plains", "en"),
            ResourceMultiplier = 1.10m,
            ResourceBonusType = ETerrainResourceBonus.Gold
        };
        var buildingType = new BuildingType
        {
            Id = Guid.NewGuid(),
            Name = new LangStr("Market", "en"),
            BaseYieldGold = 10,
            Tier = 1,
            Chain = "economic"
        };
        var building = new Building
        {
            Id = Guid.NewGuid(),
            BuildingTypeId = buildingType.Id,
            BuildingType = buildingType,
            KingdomId = Kingdom1Id
        };
        var tile = new Tile
        {
            Id = Guid.NewGuid(),
            GameId = GameId,
            TerrainTypeId = terrainType.Id,
            TerrainType = terrainType,
            KingdomId = Kingdom1Id,
            Buildings = new List<Building> { building }
        };

        _tilesMock.Setup(t => t.GetTilesWithBuildingsAndTerrainForKingdomAsync(Kingdom1Id))
            .ReturnsAsync(new List<Tile> { tile });

        var resources = new List<KingdomResource>
        {
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Gold, Amount = 100 }
        };
        _resourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(Kingdom1Id)).ReturnsAsync(resources);

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IncomeApplied.ShouldNotBeNull();
        result.Value.IncomeApplied!.ShouldContainKey("Gold");
        resources[0].Amount.ShouldBeGreaterThan(100); // income was added

        _turnLogsMock.Verify(t => t.AddAsync(It.Is<TurnLog>(
            tl => tl.EventType == EEventType.IncomeReceived)), Times.Once);
    }

    // -------------------------------------------------------------------------
    // CommitAsync test
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurnAsync_Success_CallsCommitAsync()
    {
        var game = CreateGame();
        SetupGuardSuccess(game);
        var k1 = CreateKingdom(Kingdom1Id, 1);
        var k2 = CreateKingdom(Kingdom2Id, 2);
        SetupKingdoms(k1, k2);
        SetupFactionType();

        await _sut.EndTurnAsync(GameId, UserId);

        _unitOfWorkMock.Verify(u => u.CommitAsync(default), Times.Once);
    }
}
