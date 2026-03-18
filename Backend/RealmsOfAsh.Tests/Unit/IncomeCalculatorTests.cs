using Domain.Buildings;
using Domain.Map;
using Domain.Resources;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

[Trait("Category", "Unit")]
public class IncomeCalculatorTests
{
    // --- Helpers ---

    private static BuildingType CreateFarm() => new()
    {
        Id = Guid.NewGuid(),
        Tier = 1,
        Chain = "farm",
        BaseYieldFood = 10,
    };

    private static BuildingType CreateMarket() => new()
    {
        Id = Guid.NewGuid(),
        Tier = 1,
        Chain = "market",
        BaseYieldGold = 15,
    };

    private static TerrainType CreatePlains() => new()
    {
        Id = Guid.NewGuid(),
        ResourceMultiplier = 1.10m,
        ResourceBonusType = ETerrainResourceBonus.Food,
    };

    private static TerrainType CreateForest() => new()
    {
        Id = Guid.NewGuid(),
        ResourceMultiplier = 1.10m,
        ResourceBonusType = ETerrainResourceBonus.Wood,
    };

    private static TerrainType CreateMountain() => new()
    {
        Id = Guid.NewGuid(),
        ResourceMultiplier = 1.10m,
        ResourceBonusType = ETerrainResourceBonus.Gold,
    };

    // --- CalculateBuildingIncome ---

    [Fact]
    public void CalculateBuildingIncome_FarmOnPlains_NeutralFaction_Returns11Food()
    {
        // Farm(Food=10) on Plains(Food bonus, 1.10x), faction 1.0x -> floor(10 * 1.10 * 1.0) = 11
        var result = IncomeCalculator.CalculateBuildingIncome(CreateFarm(), CreatePlains(), 1.0m);
        result.ShouldContainKeyAndValue(EResourceType.Food, 11);
    }

    [Fact]
    public void CalculateBuildingIncome_FarmOnForest_NeutralFaction_Returns10Food()
    {
        // Farm(Food=10) on Forest(Wood bonus) -> terrain bonus does NOT apply to Food
        // floor(10 * 1.0 * 1.0) = 10
        var result = IncomeCalculator.CalculateBuildingIncome(CreateFarm(), CreateForest(), 1.0m);
        result.ShouldContainKeyAndValue(EResourceType.Food, 10);
    }

    [Fact]
    public void CalculateBuildingIncome_FarmOnPlains_IronThrone_Returns9Food()
    {
        // Farm(Food=10) on Plains(Food bonus, 1.10x), faction 0.85x -> floor(10 * 1.10 * 0.85) = floor(9.35) = 9
        var result = IncomeCalculator.CalculateBuildingIncome(CreateFarm(), CreatePlains(), 0.85m);
        result.ShouldContainKeyAndValue(EResourceType.Food, 9);
    }

    [Fact]
    public void CalculateBuildingIncome_ZeroYield_NotInOutput()
    {
        // Farm only yields food, so Gold/Wood/Stone/Mana should not be in output
        var result = IncomeCalculator.CalculateBuildingIncome(CreateFarm(), CreatePlains(), 1.0m);
        result.ShouldNotContainKey(EResourceType.Gold);
        result.ShouldNotContainKey(EResourceType.Wood);
        result.ShouldNotContainKey(EResourceType.Stone);
        result.ShouldNotContainKey(EResourceType.Mana);
    }

    [Fact]
    public void CalculateBuildingIncome_MarketOnMountain_Returns16Gold()
    {
        // Market(Gold=15) on Mountain(Gold bonus, 1.10x), faction 1.0x -> floor(15 * 1.10 * 1.0) = floor(16.5) = 16
        var result = IncomeCalculator.CalculateBuildingIncome(CreateMarket(), CreateMountain(), 1.0m);
        result.ShouldContainKeyAndValue(EResourceType.Gold, 16);
    }

    // --- CalculateKingdomIncome ---

    [Fact]
    public void CalculateKingdomIncome_MultipleBuildingsSumCorrectly()
    {
        var buildings = new List<(BuildingType, TerrainType)>
        {
            (CreateFarm(), CreatePlains()),   // Food: 11
            (CreateFarm(), CreateForest()),   // Food: 10
            (CreateMarket(), CreateMountain()), // Gold: 16
        };

        var result = IncomeCalculator.CalculateKingdomIncome(buildings, 1.0m);

        result.ShouldContainKeyAndValue(EResourceType.Food, 21); // 11 + 10
        result.ShouldContainKeyAndValue(EResourceType.Gold, 16);
    }

    [Fact]
    public void CalculateKingdomIncome_EmptyList_ReturnsEmptyDict()
    {
        var result = IncomeCalculator.CalculateKingdomIncome([], 1.0m);
        result.ShouldBeEmpty();
    }
}
