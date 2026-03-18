using Domain.Game;
using Domain.Map;
using Domain.Military;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// Pure domain-level unit tests for EliminationChecker.
/// No mocks needed -- tests create entities directly and call domain methods.
/// </summary>
[Trait("Category", "Unit")]
public class WinConditionCheckerTests
{
    private static readonly Guid Kingdom1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Kingdom2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Kingdom CreateKingdom(Guid? id = null, EKingdomStatus status = EKingdomStatus.Active, DateTime? createdAt = null)
    {
        return new Kingdom
        {
            Id = id ?? Guid.NewGuid(),
            Status = status,
            CreatedAt = createdAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    private static Game CreateEliminationGame()
    {
        return new Game
        {
            Id = Guid.NewGuid(),
            Status = EGameStatus.InProgress,
            RoundNumber = 5,
            WinCondition = EWinCondition.Elimination,
        };
    }

    // -------------------------------------------------------------------------
    // EliminationChecker tests
    // -------------------------------------------------------------------------

    [Fact]
    public void EliminationChecker_WhenTwoKingdomsActive_ReturnsNull()
    {
        var checker = new EliminationChecker();
        var game = CreateEliminationGame();
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(Kingdom1Id, EKingdomStatus.Active),
            CreateKingdom(Kingdom2Id, EKingdomStatus.Active),
        };
        var tiles = new List<Tile>();

        var result = checker.Check(game, kingdoms, tiles);

        result.ShouldBeNull();
    }

    [Fact]
    public void EliminationChecker_WhenOneKingdomRemains_ReturnsWinner()
    {
        var checker = new EliminationChecker();
        var game = CreateEliminationGame();
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(Kingdom1Id, EKingdomStatus.Active),
            CreateKingdom(Kingdom2Id, EKingdomStatus.Defeated),
        };
        var tiles = new List<Tile>();

        var result = checker.Check(game, kingdoms, tiles);

        result.ShouldNotBeNull();
        result!.WinnerKingdomId.ShouldBe(Kingdom1Id);
        result.GameOver.ShouldBeTrue();
        result.WinConditionType.ShouldBe(EWinCondition.Elimination);
    }

    [Fact]
    public void EliminationChecker_WhenZeroKingdomsActive_ReturnsNullWinner()
    {
        var checker = new EliminationChecker();
        var game = CreateEliminationGame();
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(Kingdom1Id, EKingdomStatus.Defeated),
            CreateKingdom(Kingdom2Id, EKingdomStatus.Defeated),
        };
        var tiles = new List<Tile>();

        var result = checker.Check(game, kingdoms, tiles);

        result.ShouldNotBeNull();
        result!.WinnerKingdomId.ShouldBeNull();
        result.GameOver.ShouldBeTrue();
    }
}
