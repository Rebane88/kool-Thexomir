using Domain.Game;
using Domain.Map;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// Pure domain-level unit tests for Game.CheckWinCondition().
/// Uses simple stub implementations of IWinConditionChecker rather than Moq.
/// </summary>
[Trait("Category", "Unit")]
public class GameDomainWinConditionTests
{
    private static readonly Guid WinnerKingdomId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Game CreateInProgressGame()
    {
        return new Game
        {
            Id = Guid.NewGuid(),
            Status = EGameStatus.InProgress,
            TurnNumber = 5,
            WinCondition = EWinCondition.Elimination,
            WinnerKingdomId = null,
        };
    }

    // -------------------------------------------------------------------------
    // Stub checker implementations
    // -------------------------------------------------------------------------

    private sealed class StubChecker(WinCheckResult? result) : IWinConditionChecker
    {
        public WinCheckResult? Check(Game game, IReadOnlyList<Kingdom> kingdoms, IReadOnlyList<Tile> tiles)
            => result;
    }

    // -------------------------------------------------------------------------
    // CheckWinCondition tests
    // -------------------------------------------------------------------------

    [Fact]
    public void CheckWinCondition_WhenCheckerReturnsGameOver_SetsStatusToCompleted()
    {
        var game = CreateInProgressGame();
        var checker = new StubChecker(new WinCheckResult(WinnerKingdomId, EWinCondition.Elimination, GameOver: true));

        game.CheckWinCondition(checker, [], []);

        game.Status.ShouldBe(EGameStatus.Completed);
    }

    [Fact]
    public void CheckWinCondition_WhenCheckerReturnsGameOver_SetsWinnerKingdomId()
    {
        var game = CreateInProgressGame();
        var checker = new StubChecker(new WinCheckResult(WinnerKingdomId, EWinCondition.Elimination, GameOver: true));

        game.CheckWinCondition(checker, [], []);

        game.WinnerKingdomId.ShouldBe(WinnerKingdomId);
    }

    [Fact]
    public void CheckWinCondition_WhenCheckerReturnsNull_DoesNotChangeStatus()
    {
        var game = CreateInProgressGame();
        var checker = new StubChecker(null);

        game.CheckWinCondition(checker, [], []);

        game.Status.ShouldBe(EGameStatus.InProgress);
        game.WinnerKingdomId.ShouldBeNull();
    }

    [Fact]
    public void CheckWinCondition_WhenCheckerReturnsResult_ReturnsResult()
    {
        var game = CreateInProgressGame();
        var expected = new WinCheckResult(WinnerKingdomId, EWinCondition.Elimination, GameOver: true);
        var checker = new StubChecker(expected);

        var actual = game.CheckWinCondition(checker, [], []);

        actual.ShouldNotBeNull();
        actual!.WinnerKingdomId.ShouldBe(WinnerKingdomId);
        actual.GameOver.ShouldBeTrue();
    }

    [Fact]
    public void CheckWinCondition_WhenCheckerReturnsNull_ReturnsNull()
    {
        var game = CreateInProgressGame();
        var checker = new StubChecker(null);

        var actual = game.CheckWinCondition(checker, [], []);

        actual.ShouldBeNull();
    }
}
