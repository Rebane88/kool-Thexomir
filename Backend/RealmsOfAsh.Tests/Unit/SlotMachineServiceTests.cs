using System.Text.Json;
using Application.Contracts;
using Application.Services.SlotMachine;
using Base.Contracts;
using Domain.Game;
using Domain.Resources;
using Moq;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

[Trait("Category", "Unit")]
public class SlotMachineServiceTests
{
    // Mocks
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGameGuard> _gameGuardMock = new();
    private readonly Mock<IKingdomResourceRepository> _resourcesMock = new();
    private readonly Mock<ITurnLogRepository> _turnLogsMock = new();
    private readonly SlotMachineService _sut;

    // Fixed IDs
    private static readonly Guid GameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid KingdomId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public SlotMachineServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.KingdomResources).Returns(_resourcesMock.Object);
        _unitOfWorkMock.Setup(u => u.TurnLogs).Returns(_turnLogsMock.Object);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        _sut = new SlotMachineService(_unitOfWorkMock.Object, _gameGuardMock.Object);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Game CreateGame(
        EGamePhase phase = EGamePhase.Action,
        int spinCostGold = 30,
        int remainingAp = 3,
        string? slotWeights = null) => new()
    {
        Id = GameId,
        Status = EGameStatus.InProgress,
        RoundNumber = 2,
        CurrentPhase = phase,
        CurrentTurnKingdomId = KingdomId,
        SpinCostGold = spinCostGold,
        RemainingActionPoints = remainingAp,
        SlotOutcomeWeights = slotWeights!
    };

    private static Kingdom CreateKingdom() => new()
    {
        Id = KingdomId,
        GameId = GameId,
        AppUserId = UserId,
        Status = EKingdomStatus.Active
    };

    private static List<KingdomResource> CreateResources(int gold = 100) =>
    [
        new() { Id = Guid.NewGuid(), KingdomId = KingdomId, ResourceType = EResourceType.Gold, Amount = gold },
        new() { Id = Guid.NewGuid(), KingdomId = KingdomId, ResourceType = EResourceType.Food, Amount = 50 },
        new() { Id = Guid.NewGuid(), KingdomId = KingdomId, ResourceType = EResourceType.Wood, Amount = 50 }
    ];

    private void SetupGuardSuccess(Game? game = null, Kingdom? kingdom = null)
    {
        var g = game ?? CreateGame();
        var k = kingdom ?? CreateKingdom();
        _gameGuardMock.Setup(gg => gg.ValidateAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Ok(new GameGuardContext(g, k)));
    }

    private void SetupFullSuccessPath(Game? game = null, int gold = 100)
    {
        var g = game ?? CreateGame();
        SetupGuardSuccess(g);
        var resources = CreateResources(gold);
        _resourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(KingdomId)).ReturnsAsync(resources);
    }

    // -------------------------------------------------------------------------
    // Guard failure tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SpinAsync_GuardFailure_ReturnsFailResult()
    {
        _gameGuardMock.Setup(gg => gg.ValidateAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Fail("Game not found."));

        var result = await _sut.SpinAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Game not found.");
    }

    // -------------------------------------------------------------------------
    // Validation failure tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SpinAsync_WrongPhase_ReturnsFailResult()
    {
        var game = CreateGame(phase: EGamePhase.Income);
        SetupFullSuccessPath(game);

        var result = await _sut.SpinAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Can only spin during Action Phase.");
    }

    [Fact]
    public async Task SpinAsync_InsufficientGold_ReturnsFailResult()
    {
        var game = CreateGame(spinCostGold: 30);
        SetupFullSuccessPath(game, gold: 10);

        var result = await _sut.SpinAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Insufficient Gold");
        result.Error!.ShouldContain("Need 30");
        result.Error!.ShouldContain("have 10");
    }

    // -------------------------------------------------------------------------
    // Success tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SpinAsync_Success_DeductsGoldAndAppliesOutcome()
    {
        // Force +2 outcome: weights [0,0,0,0,100] means only +2
        var game = CreateGame(slotWeights: "[0,0,0,0,100]", remainingAp: 3);
        SetupGuardSuccess(game);
        var resources = CreateResources(gold: 100);
        _resourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(KingdomId)).ReturnsAsync(resources);

        var result = await _sut.SpinAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.KingdomId.ShouldBe(KingdomId);
        result.Value.Outcome.ShouldBe(2);
        result.Value.ActionPointsAfter.ShouldBe(5); // 3 + 2
        result.Value.GoldAfter.ShouldBe(70); // 100 - 30
        result.Value.GoldSpent.ShouldBe(30);

        // Verify gold was deducted on the tracked entity
        resources.First(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(70);

        // Verify AP was updated on game entity
        game.RemainingActionPoints.ShouldBe(5);

        _unitOfWorkMock.Verify(u => u.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task SpinAsync_Success_APFlooredAtZero()
    {
        // Force -2 outcome: weights [100,0,0,0,0] means only -2
        var game = CreateGame(slotWeights: "[100,0,0,0,0]", remainingAp: 1);
        SetupGuardSuccess(game);
        var resources = CreateResources(gold: 100);
        _resourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(KingdomId)).ReturnsAsync(resources);

        var result = await _sut.SpinAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Outcome.ShouldBe(-2);
        result.Value.ActionPointsAfter.ShouldBe(0); // max(0, 1-2) = 0
        game.RemainingActionPoints.ShouldBe(0);
    }

    [Fact]
    public async Task SpinAsync_Success_CreatesTurnLogWithMetadata()
    {
        var game = CreateGame(slotWeights: "[0,0,0,0,100]");
        SetupGuardSuccess(game);
        var resources = CreateResources(gold: 100);
        _resourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(KingdomId)).ReturnsAsync(resources);

        TurnLog? capturedLog = null;
        _turnLogsMock.Setup(t => t.AddAsync(It.IsAny<TurnLog>()))
            .Callback<TurnLog>(tl => capturedLog = tl)
            .ReturnsAsync((TurnLog tl) => tl);

        await _sut.SpinAsync(GameId, UserId);

        capturedLog.ShouldNotBeNull();
        capturedLog.EventType.ShouldBe(EEventType.SlotMachineSpin);
        capturedLog.GameId.ShouldBe(GameId);
        capturedLog.KingdomId.ShouldBe(KingdomId);
        capturedLog.RoundNumber.ShouldBe(2);
        capturedLog.Description.ShouldContain("+2 AP");

        // Verify metadata JSON structure
        capturedLog.Metadata.ShouldNotBeNullOrEmpty();
        var metadata = JsonDocument.Parse(capturedLog.Metadata!);
        metadata.RootElement.GetProperty("goldSpent").GetInt32().ShouldBe(30);
        metadata.RootElement.GetProperty("outcome").GetInt32().ShouldBe(2);
        metadata.RootElement.TryGetProperty("actionsAfter", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task SpinAsync_MultipleSpinsAllowed_SequentialSpinsWork()
    {
        var game = CreateGame(slotWeights: "[0,0,100,0,0]", spinCostGold: 10, remainingAp: 3);
        SetupGuardSuccess(game);
        var resources = CreateResources(gold: 100);
        _resourcesMock.Setup(r => r.GetMutableResourcesForKingdomAsync(KingdomId)).ReturnsAsync(resources);

        // First spin
        var result1 = await _sut.SpinAsync(GameId, UserId);
        result1.IsSuccess.ShouldBeTrue();
        result1.Value!.GoldAfter.ShouldBe(90); // 100 - 10

        // Second spin (gold entity is tracked, so amount persists)
        var result2 = await _sut.SpinAsync(GameId, UserId);
        result2.IsSuccess.ShouldBeTrue();
        result2.Value!.GoldAfter.ShouldBe(80); // 90 - 10

        _unitOfWorkMock.Verify(u => u.CommitAsync(default), Times.Exactly(2));
    }

    // -------------------------------------------------------------------------
    // Verify spin uses ValidateAsync NOT ValidateActionAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SpinAsync_UsesValidateAsync_NotValidateActionAsync()
    {
        SetupFullSuccessPath();

        await _sut.SpinAsync(GameId, UserId);

        // Spin should use ValidateAsync (no AP cost for spinning)
        _gameGuardMock.Verify(g => g.ValidateAsync(GameId, UserId), Times.Once);
        _gameGuardMock.Verify(g => g.ValidateActionAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }
}
