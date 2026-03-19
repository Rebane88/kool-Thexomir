using Application.Contracts;
using Application.Services.Turn;
using Application.Services.Turn.DTOs;
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
    private readonly Mock<IArmyRepository> _armiesMock = new();
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
        _unitOfWorkMock.Setup(u => u.Armies).Returns(_armiesMock.Object);
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
        // No armies -- healing/upkeep skipped
        _armiesMock.Setup(a => a.GetArmiesWithTypeForKingdomAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<Army>());
    }

    private static FactionType CreateFactionTypeWithHealing(decimal healRateModifier = 1.0m) => new()
    {
        Id = FactionTypeId,
        Name = new LangStr("Test Faction", "en"),
        ActionPointModifier = 0,
        ResourceProductionModifier = 1.0m,
        HealRateModifier = healRateModifier
    };

    private static ArmyType CreateArmyType(int upkeepGold = 5, int upkeepFood = 3, int upkeepMana = 0) => new()
    {
        Id = Guid.NewGuid(),
        Name = new LangStr("Warrior", "en"),
        Attack = 10,
        HP = 100,
        Initiative = 5,
        UpkeepGold = upkeepGold,
        UpkeepFood = upkeepFood,
        UpkeepMana = upkeepMana,
        RequiredBuildingTypeId = Guid.NewGuid()
    };

    private static Army CreateArmy(Guid kingdomId, ArmyType armyType, int currentHP, int maxHP) => new()
    {
        Id = Guid.NewGuid(),
        ArmyTypeId = armyType.Id,
        ArmyType = armyType,
        KingdomId = kingdomId,
        BuildingId = Guid.NewGuid(),
        CurrentHP = currentHP,
        MaxHP = maxHP,
        CreatedOnRound = 1
    };

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

    // -------------------------------------------------------------------------
    // Income Phase: Army Healing tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurnAsync_IncomePhase_HealsDamagedArmies()
    {
        var game = CreateGame();
        game.HealPercent = 0.10m;
        SetupGuardSuccess(game);
        var k1 = CreateKingdom(Kingdom1Id, 1);
        SetupKingdoms(k1);
        SetupFactionType(CreateFactionTypeWithHealing());

        // No buildings (skip income)
        _tilesMock.Setup(t => t.GetTilesWithBuildingsAndTerrainForKingdomAsync(Kingdom1Id))
            .ReturnsAsync(new List<Tile>());

        // Army at 80/100 HP -- should heal to 90 (10% of 100 = 10)
        var armyType = CreateArmyType(upkeepGold: 0, upkeepFood: 0);
        var army = CreateArmy(Kingdom1Id, armyType, currentHP: 80, maxHP: 100);
        _armiesMock.Setup(a => a.GetArmiesWithTypeForKingdomAsync(Kingdom1Id))
            .ReturnsAsync(new List<Army> { army });

        // Provide enough resources so upkeep doesn't trigger disbanding
        var resources = new List<KingdomResource>
        {
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Gold, Amount = 1000 },
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Food, Amount = 1000 },
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Mana, Amount = 1000 }
        };
        _resourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(Kingdom1Id)).ReturnsAsync(resources);

        var result = await _sut.EndTurnAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        army.CurrentHP.ShouldBe(90);
        _armiesMock.Verify(a => a.UpdateAsync(army), Times.Once);
        _turnLogsMock.Verify(t => t.AddAsync(It.Is<TurnLog>(
            tl => tl.EventType == EEventType.ArmyHealed)), Times.Once);
    }

    [Fact]
    public async Task EndTurnAsync_IncomePhase_DoesNotHealFullHPArmies()
    {
        var game = CreateGame();
        game.HealPercent = 0.10m;
        SetupGuardSuccess(game);
        var k1 = CreateKingdom(Kingdom1Id, 1);
        SetupKingdoms(k1);
        SetupFactionType(CreateFactionTypeWithHealing());

        _tilesMock.Setup(t => t.GetTilesWithBuildingsAndTerrainForKingdomAsync(Kingdom1Id))
            .ReturnsAsync(new List<Tile>());

        // Army at full HP
        var armyType = CreateArmyType(upkeepGold: 0, upkeepFood: 0);
        var army = CreateArmy(Kingdom1Id, armyType, currentHP: 100, maxHP: 100);
        _armiesMock.Setup(a => a.GetArmiesWithTypeForKingdomAsync(Kingdom1Id))
            .ReturnsAsync(new List<Army> { army });

        var resources = new List<KingdomResource>
        {
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Gold, Amount = 1000 },
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Food, Amount = 1000 },
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Mana, Amount = 1000 }
        };
        _resourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(Kingdom1Id)).ReturnsAsync(resources);

        await _sut.EndTurnAsync(GameId, UserId);

        army.CurrentHP.ShouldBe(100);
        _armiesMock.Verify(a => a.UpdateAsync(It.IsAny<Army>()), Times.Never);
        _turnLogsMock.Verify(t => t.AddAsync(It.Is<TurnLog>(
            tl => tl.EventType == EEventType.ArmyHealed)), Times.Never);
    }

    // -------------------------------------------------------------------------
    // Income Phase: Army Upkeep tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EndTurnAsync_IncomePhase_DeductsUpkeepForArmies()
    {
        var game = CreateGame();
        game.HealPercent = 0.10m;
        SetupGuardSuccess(game);
        var k1 = CreateKingdom(Kingdom1Id, 1);
        SetupKingdoms(k1);
        SetupFactionType(CreateFactionTypeWithHealing());

        _tilesMock.Setup(t => t.GetTilesWithBuildingsAndTerrainForKingdomAsync(Kingdom1Id))
            .ReturnsAsync(new List<Tile>());

        var armyType = CreateArmyType(upkeepGold: 5, upkeepFood: 3, upkeepMana: 0);
        var army = CreateArmy(Kingdom1Id, armyType, currentHP: 100, maxHP: 100);
        _armiesMock.Setup(a => a.GetArmiesWithTypeForKingdomAsync(Kingdom1Id))
            .ReturnsAsync(new List<Army> { army });

        var resources = new List<KingdomResource>
        {
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Gold, Amount = 100 },
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Food, Amount = 100 },
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Mana, Amount = 100 }
        };
        _resourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(Kingdom1Id)).ReturnsAsync(resources);

        await _sut.EndTurnAsync(GameId, UserId);

        resources.First(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(95);
        resources.First(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(97);
        _turnLogsMock.Verify(t => t.AddAsync(It.Is<TurnLog>(
            tl => tl.EventType == EEventType.UpkeepPaid)), Times.Once);
    }

    [Fact]
    public async Task EndTurnAsync_IncomePhase_DisbandsMostExpensiveWhenUnaffordable()
    {
        var game = CreateGame();
        game.HealPercent = 0.10m;
        SetupGuardSuccess(game);
        var k1 = CreateKingdom(Kingdom1Id, 1);
        SetupKingdoms(k1);
        SetupFactionType(CreateFactionTypeWithHealing());

        _tilesMock.Setup(t => t.GetTilesWithBuildingsAndTerrainForKingdomAsync(Kingdom1Id))
            .ReturnsAsync(new List<Tile>());

        // Expensive army (upkeep gold=50) and cheap army (upkeep gold=2)
        var expensiveType = CreateArmyType(upkeepGold: 50, upkeepFood: 0, upkeepMana: 0);
        var cheapType = CreateArmyType(upkeepGold: 2, upkeepFood: 0, upkeepMana: 0);
        var expensiveArmy = CreateArmy(Kingdom1Id, expensiveType, currentHP: 100, maxHP: 100);
        var cheapArmy = CreateArmy(Kingdom1Id, cheapType, currentHP: 100, maxHP: 100);

        _armiesMock.Setup(a => a.GetArmiesWithTypeForKingdomAsync(Kingdom1Id))
            .ReturnsAsync(new List<Army> { expensiveArmy, cheapArmy });

        // Only 10 gold -- can't afford both (52 total), can afford cheap (2) after disbanding expensive
        var resources = new List<KingdomResource>
        {
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Gold, Amount = 10 },
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Food, Amount = 1000 },
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Mana, Amount = 1000 }
        };
        _resourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(Kingdom1Id)).ReturnsAsync(resources);

        await _sut.EndTurnAsync(GameId, UserId);

        // Expensive army should be disbanded
        _armiesMock.Verify(a => a.DeleteAsync(expensiveArmy.Id), Times.Once);
        // Cheap army should NOT be disbanded
        _armiesMock.Verify(a => a.DeleteAsync(cheapArmy.Id), Times.Never);
        // Disband log created
        _turnLogsMock.Verify(t => t.AddAsync(It.Is<TurnLog>(
            tl => tl.EventType == EEventType.ArmyDisbanded)), Times.Once);
        // Upkeep still paid for remaining
        _turnLogsMock.Verify(t => t.AddAsync(It.Is<TurnLog>(
            tl => tl.EventType == EEventType.UpkeepPaid)), Times.Once);
        // Resources deducted for cheap army
        resources.First(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(8);
    }

    [Fact]
    public async Task EndTurnAsync_IncomePhase_HealingHappensBeforeUpkeep()
    {
        // A scenario where a damaged army's healing happens before upkeep is checked.
        // This test verifies the correct ordering: income -> heal -> upkeep.
        var game = CreateGame();
        game.HealPercent = 0.10m;
        SetupGuardSuccess(game);
        var k1 = CreateKingdom(Kingdom1Id, 1);
        SetupKingdoms(k1);
        SetupFactionType(CreateFactionTypeWithHealing());

        _tilesMock.Setup(t => t.GetTilesWithBuildingsAndTerrainForKingdomAsync(Kingdom1Id))
            .ReturnsAsync(new List<Tile>());

        // Damaged army should be healed first, then upkeep deducted
        var armyType = CreateArmyType(upkeepGold: 5, upkeepFood: 0, upkeepMana: 0);
        var army = CreateArmy(Kingdom1Id, armyType, currentHP: 50, maxHP: 100);
        _armiesMock.Setup(a => a.GetArmiesWithTypeForKingdomAsync(Kingdom1Id))
            .ReturnsAsync(new List<Army> { army });

        var resources = new List<KingdomResource>
        {
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Gold, Amount = 100 },
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Food, Amount = 100 },
            new() { Id = Guid.NewGuid(), KingdomId = Kingdom1Id, ResourceType = EResourceType.Mana, Amount = 100 }
        };
        _resourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(Kingdom1Id)).ReturnsAsync(resources);

        await _sut.EndTurnAsync(GameId, UserId);

        // Healing happened (50 + 10% of 100 = 60)
        army.CurrentHP.ShouldBe(60);
        // Then upkeep was deducted
        resources.First(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(95);
        // Both logs present
        _turnLogsMock.Verify(t => t.AddAsync(It.Is<TurnLog>(
            tl => tl.EventType == EEventType.ArmyHealed)), Times.Once);
        _turnLogsMock.Verify(t => t.AddAsync(It.Is<TurnLog>(
            tl => tl.EventType == EEventType.UpkeepPaid)), Times.Once);
    }
}
