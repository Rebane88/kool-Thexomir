using Domain.Buildings;
using Domain.Game;
using Domain.Map;
using Domain.Resources;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// Pure domain-level unit tests for Game aggregate behavior.
/// No mocks needed -- tests create entities directly and call domain methods.
/// </summary>
public class GameDomainTests
{
    private static readonly Guid Kingdom1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Kingdom2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Kingdom3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static Game CreateGameWithKingdoms(
        Guid currentTurnKingdomId,
        int turnNumber = 1,
        Guid? eliminatedId = null)
    {
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Status = EGameStatus.InProgress,
            TurnNumber = turnNumber,
            CurrentTurnKingdomId = currentTurnKingdomId,
            Kingdoms = new List<Kingdom>
            {
                new()
                {
                    Id = Kingdom1Id, IsEliminated = eliminatedId == Kingdom1Id,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    Id = Kingdom2Id, IsEliminated = eliminatedId == Kingdom2Id,
                    CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    Id = Kingdom3Id, IsEliminated = eliminatedId == Kingdom3Id,
                    CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                },
            },
        };
        return game;
    }

    // -------------------------------------------------------------------------
    // AdvanceTurn tests
    // -------------------------------------------------------------------------

    [Fact]
    public void AdvanceTurn_CurrentIsFirst_AdvancesToSecond()
    {
        var game = CreateGameWithKingdoms(Kingdom1Id);

        var next = game.AdvanceTurn();

        next.Id.ShouldBe(Kingdom2Id);
        game.CurrentTurnKingdomId.ShouldBe(Kingdom2Id);
        game.TurnNumber.ShouldBe(1); // no wrap
    }

    [Fact]
    public void AdvanceTurn_CurrentIsLast_WrapsToFirstAndIncrementsTurn()
    {
        var game = CreateGameWithKingdoms(Kingdom3Id, turnNumber: 1);

        var next = game.AdvanceTurn();

        next.Id.ShouldBe(Kingdom1Id);
        game.CurrentTurnKingdomId.ShouldBe(Kingdom1Id);
        game.TurnNumber.ShouldBe(2); // wrapped
    }

    [Fact]
    public void AdvanceTurn_MiddleEliminated_SkipsToThird()
    {
        var game = CreateGameWithKingdoms(Kingdom1Id, eliminatedId: Kingdom2Id);

        var next = game.AdvanceTurn();

        next.Id.ShouldBe(Kingdom3Id);
        game.CurrentTurnKingdomId.ShouldBe(Kingdom3Id);
        game.TurnNumber.ShouldBe(1); // no wrap
    }

    [Fact]
    public void AdvanceTurn_OnlyOneActive_StaysOnSameKingdom()
    {
        // Two kingdoms eliminated, only Kingdom2 is active
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Status = EGameStatus.InProgress,
            TurnNumber = 1,
            CurrentTurnKingdomId = Kingdom2Id,
            Kingdoms = new List<Kingdom>
            {
                new()
                {
                    Id = Kingdom1Id, IsEliminated = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    Id = Kingdom2Id, IsEliminated = false,
                    CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                },
                new()
                {
                    Id = Kingdom3Id, IsEliminated = true,
                    CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc),
                },
            },
        };

        var next = game.AdvanceTurn();

        next.Id.ShouldBe(Kingdom2Id);
        game.CurrentTurnKingdomId.ShouldBe(Kingdom2Id);
        game.TurnNumber.ShouldBe(2); // wraps to same = increment
    }

    // -------------------------------------------------------------------------
    // CalculateIncome tests
    // -------------------------------------------------------------------------

    [Fact]
    public void CalculateIncome_BuildingOnMatchingTerrain_AppliesTerrainBonus()
    {
        // Capital: 5 food, 2 wood, 2 stone, 5 gold, 1 mana on Plains (food bonus)
        var tiles = new List<Tile>
        {
            new()
            {
                TerrainType = new TerrainType { ResourceBonusType = ETerrainResourceBonus.Food },
                Buildings = new List<Building>
                {
                    new()
                    {
                        BuildingType = new BuildingType
                        {
                            FoodYield = 5, WoodYield = 2, StoneYield = 2,
                            GoldYield = 5, ManaYield = 1,
                        },
                    },
                },
            },
        };

        var income = Game.CalculateIncome(tiles, new Dictionary<EResourceType, decimal>());

        // Food: floor(5 * 1.25) = 6, others at 1.0x
        income[EResourceType.Food].ShouldBe(6);
        income[EResourceType.Wood].ShouldBe(2);
        income[EResourceType.Stone].ShouldBe(2);
        income[EResourceType.Gold].ShouldBe(5);
        income[EResourceType.Mana].ShouldBe(1);
    }

    [Fact]
    public void CalculateIncome_TerrainAndFactionBonusStacked()
    {
        // Building with WoodYield=10, terrain Wood bonus (1.25x), faction 1.2 for Wood
        var tiles = new List<Tile>
        {
            new()
            {
                TerrainType = new TerrainType { ResourceBonusType = ETerrainResourceBonus.Wood },
                Buildings = new List<Building>
                {
                    new()
                    {
                        BuildingType = new BuildingType { WoodYield = 10 },
                    },
                },
            },
        };
        var factionBonuses = new Dictionary<EResourceType, decimal>
        {
            { EResourceType.Wood, 1.2m },
        };

        var income = Game.CalculateIncome(tiles, factionBonuses);

        // floor(10 * 1.25 * 1.2) = floor(15.0) = 15
        income[EResourceType.Wood].ShouldBe(15);
    }

    [Fact]
    public void CalculateIncome_ZeroBuildings_AllZero()
    {
        var income = Game.CalculateIncome(
            Enumerable.Empty<Tile>(),
            new Dictionary<EResourceType, decimal>());

        income[EResourceType.Gold].ShouldBe(0);
        income[EResourceType.Food].ShouldBe(0);
        income[EResourceType.Wood].ShouldBe(0);
        income[EResourceType.Stone].ShouldBe(0);
        income[EResourceType.Mana].ShouldBe(0);
    }
}
