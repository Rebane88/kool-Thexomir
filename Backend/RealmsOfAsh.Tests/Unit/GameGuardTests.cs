using Application.Contracts;
using Application.Services.Guard;
using Domain.Game;
using Moq;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

public class GameGuardTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGameRepository> _gamesMock = new();
    private readonly Mock<IKingdomRepository> _kingdomsMock = new();
    private readonly GameGuard _sut;

    private static readonly Guid GameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid KingdomId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid OtherKingdomId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public GameGuardTests()
    {
        _unitOfWorkMock.Setup(u => u.Games).Returns(_gamesMock.Object);
        _unitOfWorkMock.Setup(u => u.Kingdoms).Returns(_kingdomsMock.Object);
        _sut = new GameGuard(_unitOfWorkMock.Object);
    }

    private Game CreateValidGame() => new()
    {
        Id = GameId,
        Status = EGameStatus.InProgress,
        CurrentTurnKingdomId = KingdomId,
    };

    private Kingdom CreateValidKingdom() => new()
    {
        Id = KingdomId,
        GameId = GameId,
        AppUserId = UserId,
        Status = EKingdomStatus.Active,
    };

    [Fact]
    public async Task ValidateAsync_GameNotFound_ReturnsFailure()
    {
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync((Game?)null);

        var result = await _sut.ValidateAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Game not found.");
    }

    [Fact]
    public async Task ValidateAsync_GameNotInProgress_ReturnsFailure()
    {
        var game = CreateValidGame();
        game.Status = EGameStatus.Lobby;
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);

        var result = await _sut.ValidateAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Game is not in progress.");
    }

    [Fact]
    public async Task ValidateAsync_UserNotInGame_ReturnsFailure()
    {
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(CreateValidGame());
        _kingdomsMock.Setup(k => k.GetKingdomByUserAndGameAsync(UserId, GameId)).ReturnsAsync((Kingdom?)null);

        var result = await _sut.ValidateAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("You are not in this game.");
    }

    [Fact]
    public async Task ValidateAsync_KingdomEliminated_ReturnsFailure()
    {
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(CreateValidGame());
        var kingdom = CreateValidKingdom();
        kingdom.Status = EKingdomStatus.Defeated;
        _kingdomsMock.Setup(k => k.GetKingdomByUserAndGameAsync(UserId, GameId)).ReturnsAsync(kingdom);

        var result = await _sut.ValidateAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Your kingdom has been eliminated.");
    }

    [Fact]
    public async Task ValidateAsync_NotPlayersTurn_ReturnsFailure()
    {
        var game = CreateValidGame();
        game.CurrentTurnKingdomId = OtherKingdomId;
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomByUserAndGameAsync(UserId, GameId)).ReturnsAsync(CreateValidKingdom());

        var result = await _sut.ValidateAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("It is not your turn.");
    }

    [Fact]
    public async Task ValidateAsync_AllValid_ReturnsSuccess()
    {
        var game = CreateValidGame();
        var kingdom = CreateValidKingdom();
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomByUserAndGameAsync(UserId, GameId)).ReturnsAsync(kingdom);

        var result = await _sut.ValidateAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Game.ShouldBe(game);
        result.Value.Kingdom.ShouldBe(kingdom);
    }

    // -------------------------------------------------------------------------
    // ValidateActionAsync tests
    // -------------------------------------------------------------------------

    private void SetupValidGameAndKingdom(Game? game = null, Kingdom? kingdom = null)
    {
        var g = game ?? CreateValidGame();
        var k = kingdom ?? CreateValidKingdom();
        _gamesMock.Setup(x => x.GetByIdWithLockAsync(GameId)).ReturnsAsync(g);
        _kingdomsMock.Setup(x => x.GetKingdomByUserAndGameAsync(UserId, GameId)).ReturnsAsync(k);
    }

    [Fact]
    public async Task ValidateActionAsync_WhenValidateAsyncFails_ReturnsFailure()
    {
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync((Game?)null);

        var result = await _sut.ValidateActionAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Game not found.");
    }

    [Fact]
    public async Task ValidateActionAsync_WhenTurnExpired_ReturnsFailure()
    {
        var game = CreateValidGame();
        game.CurrentPhase = EGamePhase.Action;
        game.RemainingActionPoints = 3;
        game.TurnDeadline = DateTime.UtcNow.AddMinutes(-5); // expired
        SetupValidGameAndKingdom(game);

        var result = await _sut.ValidateActionAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Your turn has expired.");
    }

    [Fact]
    public async Task ValidateActionAsync_WhenNotActionPhase_ReturnsFailure()
    {
        var game = CreateValidGame();
        game.CurrentPhase = EGamePhase.Income;
        game.RemainingActionPoints = 3;
        game.TurnDeadline = null;
        SetupValidGameAndKingdom(game);

        var result = await _sut.ValidateActionAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Actions can only be performed during Action Phase.");
    }

    [Fact]
    public async Task ValidateActionAsync_WhenRemainingAPIsNull_ReturnsFailure()
    {
        var game = CreateValidGame();
        game.CurrentPhase = EGamePhase.Action;
        game.RemainingActionPoints = null;
        game.TurnDeadline = null;
        SetupValidGameAndKingdom(game);

        var result = await _sut.ValidateActionAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("No action points remaining.");
    }

    [Fact]
    public async Task ValidateActionAsync_WhenRemainingAPIsZero_ReturnsFailure()
    {
        var game = CreateValidGame();
        game.CurrentPhase = EGamePhase.Action;
        game.RemainingActionPoints = 0;
        game.TurnDeadline = null;
        SetupValidGameAndKingdom(game);

        var result = await _sut.ValidateActionAsync(GameId, UserId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("No action points remaining.");
    }

    [Fact]
    public async Task ValidateActionAsync_Success_DecrementsAP()
    {
        var game = CreateValidGame();
        game.CurrentPhase = EGamePhase.Action;
        game.RemainingActionPoints = 3;
        game.TurnDeadline = null;
        SetupValidGameAndKingdom(game);

        var result = await _sut.ValidateActionAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        game.RemainingActionPoints.ShouldBe(2);
    }

    [Fact]
    public async Task ValidateActionAsync_LastAP_DecrementsToZero()
    {
        var game = CreateValidGame();
        game.CurrentPhase = EGamePhase.Action;
        game.RemainingActionPoints = 1;
        game.TurnDeadline = null;
        SetupValidGameAndKingdom(game);

        var result = await _sut.ValidateActionAsync(GameId, UserId);

        result.IsSuccess.ShouldBeTrue();
        game.RemainingActionPoints.ShouldBe(0);
    }
}
