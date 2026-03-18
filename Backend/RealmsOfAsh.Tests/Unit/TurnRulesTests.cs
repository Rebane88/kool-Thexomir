using Domain.Game;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

[Trait("Category", "Unit")]
public class TurnRulesTests
{
    private static readonly Guid KingdomId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid KingdomId2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid KingdomId3 = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    // --- Helpers ---

    private static Kingdom CreateKingdom(Guid id, int turnOrder, EKingdomStatus status = EKingdomStatus.Active) => new()
    {
        Id = id,
        TurnOrder = turnOrder,
        Status = status,
    };

    // --- GetNextPhase ---

    [Fact]
    public void GetNextPhase_Action_ReturnsBattle()
    {
        TurnRules.GetNextPhase(EGamePhase.Action).ShouldBe(EGamePhase.Battle);
    }

    [Fact]
    public void GetNextPhase_Battle_ReturnsIncome()
    {
        TurnRules.GetNextPhase(EGamePhase.Battle).ShouldBe(EGamePhase.Income);
    }

    [Fact]
    public void GetNextPhase_Income_ReturnsRoundEnd()
    {
        TurnRules.GetNextPhase(EGamePhase.Income).ShouldBe(EGamePhase.RoundEnd);
    }

    [Fact]
    public void GetNextPhase_RoundEnd_ReturnsAction()
    {
        TurnRules.GetNextPhase(EGamePhase.RoundEnd).ShouldBe(EGamePhase.Action);
    }

    // --- CalculateActionPoints ---

    [Theory]
    [InlineData(4, 0, 4)]   // base case, no faction modifier
    [InlineData(4, 1, 5)]   // Mage Council: +1 AP
    [InlineData(4, -1, 3)]  // Forest Elves: -1 AP
    [InlineData(1, -2, 0)]  // floors at 0, never negative
    [InlineData(0, 0, 0)]   // zero base
    public void CalculateActionPoints_ReturnsExpected(int baseAp, int modifier, int expected)
    {
        TurnRules.CalculateActionPoints(baseAp, modifier).ShouldBe(expected);
    }

    // --- ValidateEndTurn ---

    [Fact]
    public void ValidateEndTurn_NotActionPhase_ReturnsError()
    {
        var result = TurnRules.ValidateEndTurn(EGamePhase.Battle, KingdomId1, KingdomId1);
        result.ShouldBe("Can only end turn during Action Phase.");
    }

    [Fact]
    public void ValidateEndTurn_NotYourTurn_ReturnsError()
    {
        var result = TurnRules.ValidateEndTurn(EGamePhase.Action, KingdomId2, KingdomId1);
        result.ShouldBe("It is not your turn.");
    }

    [Fact]
    public void ValidateEndTurn_ValidState_ReturnsNull()
    {
        var result = TurnRules.ValidateEndTurn(EGamePhase.Action, KingdomId1, KingdomId1);
        result.ShouldBeNull();
    }

    // --- GetNextActiveKingdom ---

    [Fact]
    public void GetNextActiveKingdom_ThreeActive_ReturnsNextByTurnOrder()
    {
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(KingdomId1, 1),
            CreateKingdom(KingdomId2, 2),
            CreateKingdom(KingdomId3, 3),
        };
        var result = TurnRules.GetNextActiveKingdom(kingdoms, 1);
        result.ShouldNotBeNull();
        result.Id.ShouldBe(KingdomId2);
    }

    [Fact]
    public void GetNextActiveKingdom_SkipsDefeated_ReturnsNextActive()
    {
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(KingdomId1, 1),
            CreateKingdom(KingdomId2, 2, EKingdomStatus.Defeated),
            CreateKingdom(KingdomId3, 3),
        };
        var result = TurnRules.GetNextActiveKingdom(kingdoms, 1);
        result.ShouldNotBeNull();
        result.Id.ShouldBe(KingdomId3);
    }

    [Fact]
    public void GetNextActiveKingdom_LastPlayer_ReturnsNull()
    {
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(KingdomId1, 1),
        };
        var result = TurnRules.GetNextActiveKingdom(kingdoms, 1);
        result.ShouldBeNull();
    }

    [Fact]
    public void GetNextActiveKingdom_AllPlayersGone_ReturnsNull()
    {
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(KingdomId1, 1),
            CreateKingdom(KingdomId2, 2),
        };
        var result = TurnRules.GetNextActiveKingdom(kingdoms, 2);
        result.ShouldBeNull();
    }

    [Fact]
    public void GetNextActiveKingdom_AllDefeatedExceptCurrent_ReturnsNull()
    {
        var kingdoms = new List<Kingdom>
        {
            CreateKingdom(KingdomId1, 1),
            CreateKingdom(KingdomId2, 2, EKingdomStatus.Defeated),
            CreateKingdom(KingdomId3, 3, EKingdomStatus.Defeated),
        };
        var result = TurnRules.GetNextActiveKingdom(kingdoms, 1);
        result.ShouldBeNull();
    }

    // --- IsTurnExpired ---

    [Fact]
    public void IsTurnExpired_NullDeadline_ReturnsFalse()
    {
        TurnRules.IsTurnExpired(null).ShouldBeFalse();
    }

    [Fact]
    public void IsTurnExpired_FutureDeadline_ReturnsFalse()
    {
        TurnRules.IsTurnExpired(DateTime.UtcNow.AddMinutes(5)).ShouldBeFalse();
    }

    [Fact]
    public void IsTurnExpired_PastDeadline_ReturnsTrue()
    {
        TurnRules.IsTurnExpired(DateTime.UtcNow.AddMinutes(-5)).ShouldBeTrue();
    }
}
