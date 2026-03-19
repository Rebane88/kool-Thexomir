using Application.Contracts;
using Application.Services.Army;
using Application.Services.Army.DTOs;
using Base;
using Base.Contracts;
using Domain.Buildings;
using Domain.Factions;
using Domain.Game;
using Domain.Military;
using Domain.Resources;
using Moq;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

[Trait("Category", "Unit")]
public class ArmyServiceTests
{
    // Mocks
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGameGuard> _gameGuardMock = new();
    private readonly Mock<IArmyRepository> _armiesMock = new();
    private readonly Mock<IArmyTypeRepository> _armyTypesMock = new();
    private readonly Mock<IBuildingRepository> _buildingsMock = new();
    private readonly Mock<IBuildingTypeRepository> _buildingTypesMock = new();
    private readonly Mock<IKingdomResourceRepository> _resourcesMock = new();
    private readonly Mock<IFactionTypeRepository> _factionTypesMock = new();
    private readonly Mock<ITurnLogRepository> _turnLogsMock = new();
    private readonly ArmyService _sut;

    // Fixed IDs
    private static readonly Guid GameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid KingdomId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid FactionTypeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid BuildingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ArmyTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid BuildingTypeId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public ArmyServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Armies).Returns(_armiesMock.Object);
        _unitOfWorkMock.Setup(u => u.ArmyTypes).Returns(_armyTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.Buildings).Returns(_buildingsMock.Object);
        _unitOfWorkMock.Setup(u => u.BuildingTypes).Returns(_buildingTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.KingdomResources).Returns(_resourcesMock.Object);
        _unitOfWorkMock.Setup(u => u.FactionTypes).Returns(_factionTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.TurnLogs).Returns(_turnLogsMock.Object);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        _sut = new ArmyService(_unitOfWorkMock.Object, _gameGuardMock.Object);
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

    private static Building CreateBuilding() => new()
    {
        Id = BuildingId,
        BuildingTypeId = BuildingTypeId,
        KingdomId = KingdomId,
        BuiltOnRound = 1,
        BuiltAt = DateTime.UtcNow
    };

    private static BuildingType CreateBuildingType(int armyCapacity = 3, int tier = 1) => new()
    {
        Id = BuildingTypeId,
        Name = new LangStr("Barracks", "en"),
        Tier = tier,
        Chain = "Military",
        ArmyCapacity = armyCapacity
    };

    private static ArmyType CreateArmyType(int hp = 100, int trainingCostGold = 50, int trainingCostFood = 20) => new()
    {
        Id = ArmyTypeId,
        Name = new LangStr("Swordsman", "en"),
        Attack = 10,
        HP = hp,
        Initiative = 5,
        DamageRangeMin = 0.8m,
        DamageRangeMax = 1.2m,
        ChipDamageRangeMin = 0.1m,
        ChipDamageRangeMax = 0.3m,
        TrainingCostGold = trainingCostGold,
        TrainingCostFood = trainingCostFood,
        TrainingCostStone = 0,
        TrainingCostMana = 0,
        UpkeepGold = 5,
        UpkeepFood = 3,
        UpkeepMana = 0,
        RequiredBuildingTypeId = BuildingTypeId
    };

    private static FactionType CreateFactionType(decimal hpModifier = 1.0m, decimal trainingCostModifier = 1.0m) => new()
    {
        Id = FactionTypeId,
        Name = new LangStr("Test Faction", "en"),
        HPModifier = hpModifier,
        TrainingCostModifier = trainingCostModifier,
        BuildingCostModifier = 1.0m
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

    private static TrainArmyRequest CreateRequest() => new()
    {
        BuildingId = BuildingId,
        ArmyTypeId = ArmyTypeId
    };

    private void SetupSuccessfulGuard()
    {
        var game = CreateGame();
        var kingdom = CreateKingdom();
        _gameGuardMock.Setup(g => g.ValidateActionAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Ok(new GameGuardContext(game, kingdom)));
    }

    private void SetupFullSuccessPath(
        Building? building = null,
        BuildingType? buildingType = null,
        ArmyType? armyType = null,
        FactionType? factionType = null,
        List<KingdomResource>? resources = null,
        int armyCount = 0)
    {
        SetupSuccessfulGuard();

        var b = building ?? CreateBuilding();
        var bt = buildingType ?? CreateBuildingType();
        var at = armyType ?? CreateArmyType();
        var ft = factionType ?? CreateFactionType();
        var r = resources ?? CreateResources();

        _buildingsMock.Setup(x => x.GetByIdAsync(b.Id)).ReturnsAsync(b);
        _buildingTypesMock.Setup(x => x.GetByIdAsync(bt.Id)).ReturnsAsync(bt);
        _armyTypesMock.Setup(x => x.GetByIdAsync(at.Id)).ReturnsAsync(at);
        // Required building type for tier comparison -- same as buildingType in simple case
        _buildingTypesMock.Setup(x => x.GetByIdAsync(at.RequiredBuildingTypeId)).ReturnsAsync(bt);
        _armiesMock.Setup(x => x.GetArmyCountForBuildingAsync(b.Id)).ReturnsAsync(armyCount);
        _factionTypesMock.Setup(x => x.GetByIdAsync(FactionTypeId)).ReturnsAsync(ft);
        _resourcesMock.Setup(x => x.GetMutableResourcesForKingdomAsync(KingdomId)).ReturnsAsync(r);
    }

    // -------------------------------------------------------------------------
    // Failure tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TrainArmy_FailsWhenGameGuardFails()
    {
        _gameGuardMock.Setup(g => g.ValidateActionAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Fail("It is not your turn."));

        var result = await _sut.TrainArmyAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("not your turn");
    }

    [Fact]
    public async Task TrainArmy_FailsWhenBuildingNotFound()
    {
        SetupSuccessfulGuard();
        _buildingsMock.Setup(x => x.GetByIdAsync(BuildingId)).ReturnsAsync((Building?)null);

        var result = await _sut.TrainArmyAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Building not found.");
    }

    [Fact]
    public async Task TrainArmy_FailsWhenBuildingNotOwned()
    {
        SetupSuccessfulGuard();
        var building = CreateBuilding();
        building.KingdomId = Guid.NewGuid(); // different kingdom
        _buildingsMock.Setup(x => x.GetByIdAsync(BuildingId)).ReturnsAsync(building);

        var result = await _sut.TrainArmyAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Building does not belong to your kingdom.");
    }

    [Fact]
    public async Task TrainArmy_FailsWhenArmyTypeNotFound()
    {
        SetupSuccessfulGuard();
        _buildingsMock.Setup(x => x.GetByIdAsync(BuildingId)).ReturnsAsync(CreateBuilding());
        _buildingTypesMock.Setup(x => x.GetByIdAsync(BuildingTypeId)).ReturnsAsync(CreateBuildingType());
        _armyTypesMock.Setup(x => x.GetByIdAsync(ArmyTypeId)).ReturnsAsync((ArmyType?)null);

        var result = await _sut.TrainArmyAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Army type not found.");
    }

    [Fact]
    public async Task TrainArmy_FailsWhenCapacityFull()
    {
        SetupFullSuccessPath(armyCount: 3); // capacity is 3

        var result = await _sut.TrainArmyAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("full army capacity");
    }

    [Fact]
    public async Task TrainArmy_FailsWhenInsufficientResources()
    {
        var resources = CreateResources(gold: 0, food: 0);
        SetupFullSuccessPath(resources: resources);

        var result = await _sut.TrainArmyAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Insufficient");
    }

    // -------------------------------------------------------------------------
    // Success tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task TrainArmy_Success_CreatesArmyWithCorrectMaxHP()
    {
        var factionType = CreateFactionType(hpModifier: 1.2m);
        var armyType = CreateArmyType(hp: 100);
        SetupFullSuccessPath(factionType: factionType, armyType: armyType);

        var result = await _sut.TrainArmyAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value!.MaxHP.ShouldBe(120); // 100 * 1.2
        result.Value.CurrentHP.ShouldBe(120);
        _armiesMock.Verify(r => r.AddAsync(It.Is<Army>(
            a => a.MaxHP == 120 && a.CurrentHP == 120)), Times.Once);
    }

    [Fact]
    public async Task TrainArmy_Success_DeductsResourcesWithFactionModifier()
    {
        var factionType = CreateFactionType(trainingCostModifier: 1.5m);
        var armyType = CreateArmyType(trainingCostGold: 50, trainingCostFood: 20);
        var resources = CreateResources(gold: 500, food: 500);
        SetupFullSuccessPath(factionType: factionType, armyType: armyType, resources: resources);

        var result = await _sut.TrainArmyAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeTrue();
        // Gold: ceil(50 * 1.5) = 75, Food: ceil(20 * 1.5) = 30
        result.Value!.ResourcesAfter["Gold"].ShouldBe(425); // 500 - 75
        result.Value.ResourcesAfter["Food"].ShouldBe(470); // 500 - 30
    }

    [Fact]
    public async Task TrainArmy_Success_CreatesTurnLogWithArmyTrainedEvent()
    {
        SetupFullSuccessPath();

        await _sut.TrainArmyAsync(GameId, UserId, CreateRequest());

        _turnLogsMock.Verify(r => r.AddAsync(It.Is<TurnLog>(
            t => t.EventType == EEventType.ArmyTrained)), Times.Once);
    }

    [Fact]
    public async Task TrainArmy_Success_CommitsOnce()
    {
        SetupFullSuccessPath();

        await _sut.TrainArmyAsync(GameId, UserId, CreateRequest());

        _unitOfWorkMock.Verify(u => u.CommitAsync(default), Times.Once);
    }
}
