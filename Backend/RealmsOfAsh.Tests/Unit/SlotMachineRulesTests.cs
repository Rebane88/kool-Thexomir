using Domain.Game;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

[Trait("Category", "Unit")]
public class SlotMachineRulesTests
{
    // --- ValidateSpin ---

    [Fact]
    public void ValidateSpin_NotActionPhase_ReturnsError()
    {
        var result = SlotMachineRules.ValidateSpin(EGamePhase.Battle, 100m, 30);
        result.ShouldBe("Can only spin during Action Phase.");
    }

    [Fact]
    public void ValidateSpin_InsufficientGold_ReturnsError()
    {
        var result = SlotMachineRules.ValidateSpin(EGamePhase.Action, 20m, 30);
        result.ShouldBe("Insufficient Gold. Need 30, have 20.");
    }

    [Fact]
    public void ValidateSpin_ExactGold_ReturnsNull()
    {
        var result = SlotMachineRules.ValidateSpin(EGamePhase.Action, 30m, 30);
        result.ShouldBeNull();
    }

    [Fact]
    public void ValidateSpin_MoreThanEnoughGold_ReturnsNull()
    {
        var result = SlotMachineRules.ValidateSpin(EGamePhase.Action, 100m, 30);
        result.ShouldBeNull();
    }

    // --- DetermineOutcome ---

    [Fact]
    public void DetermineOutcome_AllWeightOnMinus2_ReturnsMinus2()
    {
        var result = SlotMachineRules.DetermineOutcome("[100,0,0,0,0]", new Random(42));
        result.ShouldBe(-2);
    }

    [Fact]
    public void DetermineOutcome_AllWeightOnPlus2_ReturnsPlus2()
    {
        var result = SlotMachineRules.DetermineOutcome("[0,0,0,0,100]", new Random(42));
        result.ShouldBe(2);
    }

    [Fact]
    public void DetermineOutcome_AllWeightOnZero_ReturnsZero()
    {
        var result = SlotMachineRules.DetermineOutcome("[0,0,100,0,0]", new Random(42));
        result.ShouldBe(0);
    }

    [Fact]
    public void DetermineOutcome_NullWeights_UsesDefaultAndDoesNotThrow()
    {
        // Should not throw, uses default weights [5,25,30,25,15]
        var result = SlotMachineRules.DetermineOutcome(null, new Random(42));
        result.ShouldBeInRange(-2, 2);
    }

    [Fact]
    public void DetermineOutcome_InvalidJson_UsesDefault()
    {
        var result = SlotMachineRules.DetermineOutcome("not-json", new Random(42));
        result.ShouldBeInRange(-2, 2);
    }

    [Fact]
    public void DetermineOutcome_EmptyString_UsesDefault()
    {
        var result = SlotMachineRules.DetermineOutcome("", new Random(42));
        result.ShouldBeInRange(-2, 2);
    }

    // --- ApplyOutcome ---

    [Theory]
    [InlineData(3, 2, 5)]
    [InlineData(3, -1, 2)]
    [InlineData(1, -2, 0)]  // floors at 0
    [InlineData(0, -1, 0)]  // floors at 0
    [InlineData(0, 2, 2)]
    public void ApplyOutcome_ReturnsExpected(int currentAp, int outcome, int expected)
    {
        SlotMachineRules.ApplyOutcome(currentAp, outcome).ShouldBe(expected);
    }
}
