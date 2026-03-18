using Domain.Buildings;
using Domain.Map;
using Domain.Resources;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

[Trait("Category", "Unit")]
public class BuildingRulesTests
{
    private static readonly Guid KingdomId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherKingdomId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Tier1TypeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Tier2TypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // --- Helpers ---

    private static Tile CreateOwnedTile(bool isCastle = false) => new()
    {
        Id = Guid.NewGuid(),
        KingdomId = KingdomId,
        IsCastle = isCastle,
    };

    private static BuildingType CreateTier1Type() => new()
    {
        Id = Tier1TypeId,
        Tier = 1,
        Chain = "farm",
        CostGold = 100,
        CostFood = 50,
        CostWood = 30,
        CostStone = 0,
        CostMana = 0,
    };

    private static BuildingType CreateTier2Type() => new()
    {
        Id = Tier2TypeId,
        Tier = 2,
        Chain = "farm",
        UnlockedByBuildingTypeId = Tier1TypeId,
        CostGold = 200,
        CostFood = 100,
        CostWood = 60,
        CostStone = 20,
        CostMana = 0,
    };

    private static Building CreateExistingBuilding(Guid buildingTypeId) => new()
    {
        Id = Guid.NewGuid(),
        BuildingTypeId = buildingTypeId,
        KingdomId = KingdomId,
    };

    private static List<KingdomResource> CreateResources(int gold, int food, int wood, int stone, int mana) =>
    [
        new() { KingdomId = KingdomId, ResourceType = EResourceType.Gold, Amount = gold },
        new() { KingdomId = KingdomId, ResourceType = EResourceType.Food, Amount = food },
        new() { KingdomId = KingdomId, ResourceType = EResourceType.Wood, Amount = wood },
        new() { KingdomId = KingdomId, ResourceType = EResourceType.Stone, Amount = stone },
        new() { KingdomId = KingdomId, ResourceType = EResourceType.Mana, Amount = mana },
    ];

    // --- ValidatePlacement ---

    [Fact]
    public void ValidatePlacement_ValidTier1_ReturnsNull()
    {
        var tile = CreateOwnedTile();
        var result = BuildingRules.ValidatePlacement(CreateTier1Type(), null, tile, KingdomId);
        result.ShouldBeNull();
    }

    [Fact]
    public void ValidatePlacement_TileNotOwned_ReturnsError()
    {
        var tile = CreateOwnedTile();
        var result = BuildingRules.ValidatePlacement(CreateTier1Type(), null, tile, OtherKingdomId);
        result.ShouldBe("Tile is not owned by your kingdom.");
    }

    [Fact]
    public void ValidatePlacement_CastleTile_ReturnsError()
    {
        var tile = CreateOwnedTile(isCastle: true);
        var result = BuildingRules.ValidatePlacement(CreateTier1Type(), null, tile, KingdomId);
        result.ShouldBe("Cannot build on castle tile.");
    }

    [Fact]
    public void ValidatePlacement_Tier1WithExistingBuilding_ReturnsError()
    {
        var tile = CreateOwnedTile();
        var existing = CreateExistingBuilding(Tier1TypeId);
        var result = BuildingRules.ValidatePlacement(CreateTier1Type(), existing, tile, KingdomId);
        result.ShouldBe("Tile already has a building. Use upgrade instead.");
    }

    [Fact]
    public void ValidatePlacement_Tier2WithNoExisting_ReturnsError()
    {
        var tile = CreateOwnedTile();
        var result = BuildingRules.ValidatePlacement(CreateTier2Type(), null, tile, KingdomId);
        result.ShouldBe("Tile has no building to upgrade.");
    }

    [Fact]
    public void ValidatePlacement_Tier2WithWrongPrerequisite_ReturnsError()
    {
        var tile = CreateOwnedTile();
        var wrongBuilding = CreateExistingBuilding(Guid.NewGuid()); // wrong type
        var result = BuildingRules.ValidatePlacement(CreateTier2Type(), wrongBuilding, tile, KingdomId);
        result.ShouldBe("Existing building is not the prerequisite for this upgrade.");
    }

    [Fact]
    public void ValidatePlacement_ValidTier2Upgrade_ReturnsNull()
    {
        var tile = CreateOwnedTile();
        var existing = CreateExistingBuilding(Tier1TypeId);
        var result = BuildingRules.ValidatePlacement(CreateTier2Type(), existing, tile, KingdomId);
        result.ShouldBeNull();
    }

    // --- ApplyFactionCostModifier ---

    [Theory]
    [InlineData(100, 1.0, 100)]
    [InlineData(100, 0.80, 80)]
    [InlineData(100, 0.85, 85)]
    [InlineData(7, 0.85, 6)]   // ceil(5.95) = 6
    [InlineData(0, 0.85, 0)]   // zero base cost
    [InlineData(3, 1.15, 4)]   // ceil(3.45) = 4
    public void ApplyFactionCostModifier_ReturnsCorrectValue(int baseCost, double modifier, int expected)
    {
        var result = BuildingRules.ApplyFactionCostModifier(baseCost, (decimal)modifier);
        result.ShouldBe(expected);
    }

    // --- ApplyTrainingCostModifier ---

    [Fact]
    public void ApplyTrainingCostModifier_50_Times_085_Returns43()
    {
        var result = BuildingRules.ApplyTrainingCostModifier(50, 0.85m);
        result.ShouldBe(43); // ceil(42.5) = 43
    }

    // --- CanAfford ---

    [Fact]
    public void CanAfford_SufficientResources_ReturnsTrue()
    {
        var resources = CreateResources(gold: 200, food: 100, wood: 50, stone: 10, mana: 10);
        var buildingType = CreateTier1Type(); // costs: G=100, F=50, W=30, S=0, M=0
        var result = BuildingRules.CanAfford(resources, buildingType, 1.0m);
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanAfford_InsufficientGold_ReturnsFalse()
    {
        var resources = CreateResources(gold: 50, food: 100, wood: 50, stone: 10, mana: 10);
        var buildingType = CreateTier1Type(); // costs: G=100
        var result = BuildingRules.CanAfford(resources, buildingType, 1.0m);
        result.ShouldBeFalse();
    }

    [Fact]
    public void CanAfford_ZeroCostAlwaysPasses()
    {
        // Mana cost is 0, so even 0 mana should pass
        var resources = CreateResources(gold: 200, food: 100, wood: 50, stone: 10, mana: 0);
        var buildingType = CreateTier1Type(); // CostMana=0
        var result = BuildingRules.CanAfford(resources, buildingType, 1.0m);
        result.ShouldBeTrue();
    }

    // --- DeductBuildingCost ---

    [Fact]
    public void DeductBuildingCost_Affordable_DeductsAndReturnsNull()
    {
        var resources = CreateResources(gold: 200, food: 100, wood: 50, stone: 10, mana: 10);
        var buildingType = CreateTier1Type(); // costs: G=100, F=50, W=30, S=0, M=0
        var result = BuildingRules.DeductBuildingCost(resources, buildingType, 1.0m);

        result.ShouldBeNull();
        resources.First(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(100);
        resources.First(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(50);
        resources.First(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(20);
    }

    [Fact]
    public void DeductBuildingCost_Insufficient_ReturnsErrorAndDoesNotDeduct()
    {
        var resources = CreateResources(gold: 50, food: 100, wood: 50, stone: 10, mana: 10);
        var buildingType = CreateTier1Type(); // costs: G=100
        var result = BuildingRules.DeductBuildingCost(resources, buildingType, 1.0m);

        result.ShouldNotBeNull();
        // Should NOT have deducted anything
        resources.First(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(50);
    }

    [Fact]
    public void DeductBuildingCost_WithFactionModifier_UsesCeilRounding()
    {
        // buildingType CostGold=100, modifier=0.85 -> ceil(85) = 85
        // buildingType CostFood=50, modifier=0.85 -> ceil(42.5) = 43
        var resources = CreateResources(gold: 200, food: 100, wood: 50, stone: 10, mana: 10);
        var buildingType = CreateTier1Type();
        var result = BuildingRules.DeductBuildingCost(resources, buildingType, 0.85m);

        result.ShouldBeNull();
        resources.First(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(115); // 200 - 85
        resources.First(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(57);  // 100 - 43
        resources.First(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(24);  // 50 - ceil(30*0.85=25.5)=26 -> 50-26=24
    }

    [Fact]
    public void DeductBuildingCost_ResourceFlooredAtZero()
    {
        // Exact amount = cost, should end at 0 not negative
        var resources = CreateResources(gold: 100, food: 50, wood: 30, stone: 0, mana: 0);
        var buildingType = CreateTier1Type();
        var result = BuildingRules.DeductBuildingCost(resources, buildingType, 1.0m);

        result.ShouldBeNull();
        resources.First(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(0);
        resources.First(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(0);
        resources.First(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(0);
    }

    // --- DeductResourceCost ---

    [Fact]
    public void DeductResourceCost_Affordable_DeductsAndReturnsNull()
    {
        var resources = CreateResources(gold: 100, food: 50, wood: 30, stone: 10, mana: 5);
        var costs = new Dictionary<EResourceType, int>
        {
            { EResourceType.Gold, 50 },
            { EResourceType.Food, 20 },
        };
        var result = BuildingRules.DeductResourceCost(resources, costs);

        result.ShouldBeNull();
        resources.First(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(50);
        resources.First(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(30);
    }

    [Fact]
    public void DeductResourceCost_Insufficient_ReturnsErrorWithoutDeducting()
    {
        var resources = CreateResources(gold: 10, food: 50, wood: 30, stone: 10, mana: 5);
        var costs = new Dictionary<EResourceType, int>
        {
            { EResourceType.Gold, 50 },
        };
        var result = BuildingRules.DeductResourceCost(resources, costs);

        result.ShouldNotBeNull();
        resources.First(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(10);
    }

    [Fact]
    public void DeductResourceCost_FloorsAtZero()
    {
        var resources = CreateResources(gold: 30, food: 0, wood: 0, stone: 0, mana: 0);
        var costs = new Dictionary<EResourceType, int>
        {
            { EResourceType.Gold, 30 },
        };
        var result = BuildingRules.DeductResourceCost(resources, costs);

        result.ShouldBeNull();
        resources.First(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(0);
    }
}
