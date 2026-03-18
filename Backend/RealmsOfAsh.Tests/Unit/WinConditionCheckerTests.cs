using Domain.Buildings;
using Domain.Game;
using Domain.Map;
using Domain.Military;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// Pure domain-level unit tests for EliminationChecker and ScoreChecker.
/// No mocks needed -- tests create entities directly and call domain methods.
/// </summary>
[Trait("Category", "Unit")]
public class WinConditionCheckerTests
{
    private static readonly Guid Kingdom1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Kingdom2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Kingdom3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Kingdom CreateKingdom(Guid? id = null, bool isEliminated = false, DateTime? createdAt = null)
    {
        return new Kingdom
        {
            Id = id ?? Guid.NewGuid(),
            IsEliminated = isEliminated,
            CreatedAt = createdAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Armies = new List<Army>(),
        };
    }

    private static Tile CreateTile(Guid? kingdomId = null, List<Building>? buildings = null)
    {
        return new Tile
        {
            Id = Guid.NewGuid(),
            KingdomId = kingdomId,
            Buildings = buildings ?? new List<Building>(),
        };
    }

    private static Game CreateEliminationGame()
    {
        return new Game
        {
            Id = Guid.NewGuid(),
            Status = EGameStatus.InProgress,
            TurnNumber = 5,
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
            CreateKingdom(Kingdom1Id, isEliminated: false),
            CreateKingdom(Kingdom2Id, isEliminated: false),
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
            CreateKingdom(Kingdom1Id, isEliminated: false),
            CreateKingdom(Kingdom2Id, isEliminated: true),
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
            CreateKingdom(Kingdom1Id, isEliminated: true),
            CreateKingdom(Kingdom2Id, isEliminated: true),
        };
        var tiles = new List<Tile>();

        var result = checker.Check(game, kingdoms, tiles);

        result.ShouldNotBeNull();
        result!.WinnerKingdomId.ShouldBeNull();
        result.GameOver.ShouldBeTrue();
    }

}
