using Domain.Buildings;
using Domain.Game;
using Domain.Map;
using Domain.Military;
using Shouldly;
using MilitaryUnit = Domain.Military.Unit;

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

    private static Game CreateScoreGame(int turnNumber = 10, int? maxTurnCount = 10)
    {
        return new Game
        {
            Id = Guid.NewGuid(),
            Status = EGameStatus.InProgress,
            TurnNumber = turnNumber,
            WinCondition = EWinCondition.Score,
            MaxTurnCount = maxTurnCount,
        };
    }

    private static Building CreateBuilding(int tier = 1)
    {
        return new Building
        {
            Id = Guid.NewGuid(),
            BuildingType = new BuildingType
            {
                Id = Guid.NewGuid(),
                Name = new Base.LangStr("Building", "en"),
                Tier = tier,
            },
        };
    }

    private static Army CreateArmyWithUnits(Guid kingdomId, int unitQuantity)
    {
        var army = new Army
        {
            Id = Guid.NewGuid(),
            KingdomId = kingdomId,
            Units = new List<MilitaryUnit>
            {
                new() { Id = Guid.NewGuid(), UnitTypeId = Guid.NewGuid(), Quantity = unitQuantity },
            },
        };
        return army;
    }

    // -------------------------------------------------------------------------
    // EliminationChecker tests
    // -------------------------------------------------------------------------

    [Fact]
    public void EliminationChecker_WhenWinConditionIsScore_ReturnsNull()
    {
        var checker = new EliminationChecker();
        var game = new Game { WinCondition = EWinCondition.Score };
        var kingdoms = new List<Kingdom> { CreateKingdom(Kingdom1Id) };
        var tiles = new List<Tile>();

        var result = checker.Check(game, kingdoms, tiles);

        result.ShouldBeNull();
    }

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

    // -------------------------------------------------------------------------
    // ScoreChecker tests
    // -------------------------------------------------------------------------

    [Fact]
    public void ScoreChecker_WhenWinConditionIsElimination_ReturnsNull()
    {
        var checker = new ScoreChecker();
        var game = new Game { WinCondition = EWinCondition.Elimination, TurnNumber = 15, MaxTurnCount = 10 };
        var kingdoms = new List<Kingdom> { CreateKingdom(Kingdom1Id) };
        var tiles = new List<Tile>();

        var result = checker.Check(game, kingdoms, tiles);

        result.ShouldBeNull();
    }

    [Fact]
    public void ScoreChecker_WhenMaxTurnCountIsNull_ReturnsNull()
    {
        var checker = new ScoreChecker();
        var game = CreateScoreGame(turnNumber: 15, maxTurnCount: null);
        var kingdoms = new List<Kingdom> { CreateKingdom(Kingdom1Id) };
        var tiles = new List<Tile>();

        var result = checker.Check(game, kingdoms, tiles);

        result.ShouldBeNull();
    }

    [Fact]
    public void ScoreChecker_WhenTurnNumberEqualsMaxTurnCount_ReturnsNull()
    {
        var checker = new ScoreChecker();
        var game = CreateScoreGame(turnNumber: 10, maxTurnCount: 10);
        var kingdoms = new List<Kingdom> { CreateKingdom(Kingdom1Id) };
        var tiles = new List<Tile>();

        var result = checker.Check(game, kingdoms, tiles);

        result.ShouldBeNull();
    }

    [Fact]
    public void ScoreChecker_WhenTurnNumberExceedsMaxTurnCount_ReturnsWinnerByScore()
    {
        var checker = new ScoreChecker();
        var game = CreateScoreGame(turnNumber: 11, maxTurnCount: 10);

        // Kingdom A: 3 tiles (3pts), 1 building tier 2 (4pts), 5 army units (5pts) = 12
        // Kingdom B: 2 tiles (2pts), 2 buildings tier 1 (2+2=4pts), 10 army units (10pts) = 16
        var k1 = CreateKingdom(Kingdom1Id);
        k1.Armies = new List<Army> { CreateArmyWithUnits(Kingdom1Id, 5) };

        var k2 = CreateKingdom(Kingdom2Id);
        k2.Armies = new List<Army> { CreateArmyWithUnits(Kingdom2Id, 10) };

        var tiles = new List<Tile>
        {
            CreateTile(Kingdom1Id, [CreateBuilding(tier: 2)]),
            CreateTile(Kingdom1Id),
            CreateTile(Kingdom1Id),
            CreateTile(Kingdom2Id, [CreateBuilding(tier: 1)]),
            CreateTile(Kingdom2Id, [CreateBuilding(tier: 1)]),
        };

        var kingdoms = new List<Kingdom> { k1, k2 };

        var result = checker.Check(game, kingdoms, tiles);

        result.ShouldNotBeNull();
        result!.WinnerKingdomId.ShouldBe(Kingdom2Id);
        result.GameOver.ShouldBeTrue();
        result.WinConditionType.ShouldBe(EWinCondition.Score);
    }

    [Fact]
    public void ScoreChecker_TiebreakerByTilesOwned_KingdomWithMoreTilesWins()
    {
        var checker = new ScoreChecker();
        var game = CreateScoreGame(turnNumber: 11, maxTurnCount: 10);

        // Both kingdoms have same base score via buildings + armies
        // but k2 has more tiles -> k2 wins tiebreaker
        var k1 = CreateKingdom(Kingdom1Id);
        k1.Armies = new List<Army>();

        var k2 = CreateKingdom(Kingdom2Id);
        k2.Armies = new List<Army>();

        var tiles = new List<Tile>
        {
            CreateTile(Kingdom1Id), // 1 tile = 1pt
            CreateTile(Kingdom2Id), // 2 tiles = 2pt
            CreateTile(Kingdom2Id),
        };

        var kingdoms = new List<Kingdom> { k1, k2 };

        var result = checker.Check(game, kingdoms, tiles);

        result.ShouldNotBeNull();
        result!.WinnerKingdomId.ShouldBe(Kingdom2Id);
    }

    [Fact]
    public void ScoreChecker_TiebreakerByCreatedAt_OlderKingdomWins()
    {
        var checker = new ScoreChecker();
        var game = CreateScoreGame(turnNumber: 11, maxTurnCount: 10);

        // Same score, same tile count -> tiebreaker by CreatedAt (older wins)
        var k1 = CreateKingdom(Kingdom1Id, createdAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        k1.Armies = new List<Army>();

        var k2 = CreateKingdom(Kingdom2Id, createdAt: new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        k2.Armies = new List<Army>();

        // Each has 1 tile, no buildings, no armies -> equal score
        var tiles = new List<Tile>
        {
            CreateTile(Kingdom1Id),
            CreateTile(Kingdom2Id),
        };

        var kingdoms = new List<Kingdom> { k1, k2 };

        var result = checker.Check(game, kingdoms, tiles);

        result.ShouldNotBeNull();
        result!.WinnerKingdomId.ShouldBe(Kingdom1Id); // older kingdom wins
    }

    [Fact]
    public void ScoreChecker_CalculateScore_CorrectFormula()
    {
        // Kingdom has 2 tiles, 1 building tier 3 (=6pts), 4 army units
        // Expected: 2*1 + 3*2 + 4*1 = 2 + 6 + 4 = 12
        var kingdom = CreateKingdom(Kingdom1Id);
        kingdom.Armies = new List<Army> { CreateArmyWithUnits(Kingdom1Id, 4) };

        var tiles = new List<Tile>
        {
            CreateTile(Kingdom1Id, [CreateBuilding(tier: 3)]),
            CreateTile(Kingdom1Id),
        };

        var score = ScoreChecker.CalculateScore(kingdom, tiles);

        score.ShouldBe(12);
    }
}
