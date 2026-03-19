using Domain.Buildings;
using Domain.Military;
using Domain.Resources;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

[Trait("Category", "Unit")]
public class ArmyRulesTests
{
    // Seeder GUIDs matching BuildingTypeSeeder
    private static readonly Guid BarracksId = Guid.Parse("BBBBBBBB-0001-0000-0000-000000000010");
    private static readonly Guid StablesId = Guid.Parse("BBBBBBBB-0001-0000-0000-000000000011");
    private static readonly Guid WarAcademyId = Guid.Parse("BBBBBBBB-0001-0000-0000-000000000012");

    // --- Helpers ---

    private static BuildingType CreateBarracks() => new()
    {
        Id = BarracksId,
        Tier = 1,
        Chain = "Military",
        ArmyCapacity = 3,
    };

    private static BuildingType CreateStables() => new()
    {
        Id = StablesId,
        Tier = 2,
        Chain = "Military",
        ArmyCapacity = 3,
        UnlockedByBuildingTypeId = BarracksId,
    };

    private static BuildingType CreateWarAcademy() => new()
    {
        Id = WarAcademyId,
        Tier = 3,
        Chain = "Military",
        ArmyCapacity = 3,
        UnlockedByBuildingTypeId = StablesId,
    };

    private static BuildingType CreateFarm() => new()
    {
        Id = Guid.NewGuid(),
        Tier = 1,
        Chain = "Farm",
        ArmyCapacity = 0,
    };

    private static ArmyType CreateWarrior() => new()
    {
        Id = Guid.NewGuid(),
        Attack = 50,
        HP = 100,
        Initiative = 10,
        DamageRangeMin = 0.8m,
        DamageRangeMax = 1.2m,
        ChipDamageRangeMin = 0.05m,
        ChipDamageRangeMax = 0.10m,
        TrainingCostGold = 100,
        TrainingCostFood = 50,
        TrainingCostStone = 0,
        TrainingCostMana = 0,
        UpkeepGold = 5,
        UpkeepFood = 3,
        UpkeepMana = 0,
        RequiredBuildingTypeId = BarracksId,
    };

    private static ArmyType CreateKnight() => new()
    {
        Id = Guid.NewGuid(),
        Attack = 80,
        HP = 150,
        Initiative = 8,
        DamageRangeMin = 0.9m,
        DamageRangeMax = 1.1m,
        ChipDamageRangeMin = 0.03m,
        ChipDamageRangeMax = 0.08m,
        TrainingCostGold = 200,
        TrainingCostFood = 100,
        TrainingCostStone = 50,
        TrainingCostMana = 0,
        UpkeepGold = 10,
        UpkeepFood = 5,
        UpkeepMana = 0,
        RequiredBuildingTypeId = StablesId,
    };

    // --- ValidateTraining ---

    [Fact]
    public void ValidateTraining_NonMilitaryBuilding_ReturnsError()
    {
        var farm = CreateFarm();
        var warrior = CreateWarrior();
        var result = ArmyRules.ValidateTraining(farm, warrior, 0, 1);
        result.ShouldNotBeNull();
        result.ShouldContain("cannot train armies");
    }

    [Fact]
    public void ValidateTraining_FullCapacity_ReturnsError()
    {
        var barracks = CreateBarracks();
        var warrior = CreateWarrior();
        var result = ArmyRules.ValidateTraining(barracks, warrior, 3, 1);
        result.ShouldNotBeNull();
        result.ShouldContain("full army capacity");
    }

    [Fact]
    public void ValidateTraining_NonMilitaryChain_ReturnsError()
    {
        var nonMilitary = CreateFarm();
        nonMilitary.ArmyCapacity = 3; // has capacity but wrong chain
        var warrior = CreateWarrior();
        var result = ArmyRules.ValidateTraining(nonMilitary, warrior, 0, 1);
        result.ShouldNotBeNull();
        result.ShouldContain("cannot train");
    }

    [Fact]
    public void ValidateTraining_ExactMatch_ReturnsNull()
    {
        var barracks = CreateBarracks();
        var warrior = CreateWarrior();
        var result = ArmyRules.ValidateTraining(barracks, warrior, 0, 1);
        result.ShouldBeNull();
    }

    [Fact]
    public void ValidateTraining_HigherTierBuilding_TrainsLowerTier_ReturnsNull()
    {
        var stables = CreateStables();
        var warrior = CreateWarrior();
        // Stables is tier 2, warrior requires tier 1 (Barracks)
        var result = ArmyRules.ValidateTraining(stables, warrior, 0, 1);
        result.ShouldBeNull();
    }

    [Fact]
    public void ValidateTraining_WarAcademy_TrainsKnight_ReturnsNull()
    {
        var warAcademy = CreateWarAcademy();
        var knight = CreateKnight();
        // War Academy is tier 3, knight requires tier 2 (Stables)
        var result = ArmyRules.ValidateTraining(warAcademy, knight, 0, 2);
        result.ShouldBeNull();
    }

    [Fact]
    public void ValidateTraining_LowerTierBuilding_CannotTrainHigherTier_ReturnsError()
    {
        var barracks = CreateBarracks();
        var knight = CreateKnight();
        // Barracks is tier 1, knight requires tier 2 (Stables)
        var result = ArmyRules.ValidateTraining(barracks, knight, 0, 2);
        result.ShouldNotBeNull();
        result.ShouldContain("cannot train");
    }

    // --- CanBuildingTrainArmyType ---

    [Fact]
    public void CanBuildingTrainArmyType_NonMilitaryChain_ReturnsFalse()
    {
        var farm = CreateFarm();
        var warrior = CreateWarrior();
        ArmyRules.CanBuildingTrainArmyType(farm, warrior, 1).ShouldBeFalse();
    }

    [Fact]
    public void CanBuildingTrainArmyType_ExactMatch_ReturnsTrue()
    {
        var barracks = CreateBarracks();
        var warrior = CreateWarrior();
        ArmyRules.CanBuildingTrainArmyType(barracks, warrior, 1).ShouldBeTrue();
    }

    [Fact]
    public void CanBuildingTrainArmyType_HigherTier_ReturnsTrue()
    {
        var stables = CreateStables();
        var warrior = CreateWarrior();
        ArmyRules.CanBuildingTrainArmyType(stables, warrior, 1).ShouldBeTrue();
    }

    [Fact]
    public void CanBuildingTrainArmyType_LowerTier_ReturnsFalse()
    {
        var barracks = CreateBarracks();
        var knight = CreateKnight();
        ArmyRules.CanBuildingTrainArmyType(barracks, knight, 2).ShouldBeFalse();
    }

    // --- CalculateMaxHP ---

    [Fact]
    public void CalculateMaxHP_WithModifier_ReturnsIntCast()
    {
        ArmyRules.CalculateMaxHP(100, 1.15m).ShouldBe(115);
    }

    [Fact]
    public void CalculateMaxHP_NoModifier_ReturnsBase()
    {
        ArmyRules.CalculateMaxHP(100, 1.0m).ShouldBe(100);
    }

    [Fact]
    public void CalculateMaxHP_ReducedModifier_ReturnsIntCast()
    {
        ArmyRules.CalculateMaxHP(100, 0.85m).ShouldBe(85);
    }

    // --- CalculateEffectiveAttack ---

    [Fact]
    public void CalculateEffectiveAttack_FullHP_ReturnsBaseAttack()
    {
        ArmyRules.CalculateEffectiveAttack(50, 100, 100).ShouldBe(50);
    }

    [Fact]
    public void CalculateEffectiveAttack_HalfHP_ReturnsHalfAttack()
    {
        ArmyRules.CalculateEffectiveAttack(50, 50, 100).ShouldBe(25);
    }

    [Fact]
    public void CalculateEffectiveAttack_ZeroMaxHP_ReturnsZero()
    {
        ArmyRules.CalculateEffectiveAttack(50, 0, 0).ShouldBe(0);
    }

    [Fact]
    public void CalculateEffectiveAttack_ZeroCurrentHP_ReturnsZero()
    {
        ArmyRules.CalculateEffectiveAttack(50, 0, 100).ShouldBe(0);
    }

    // --- GetModifiedTrainingCosts ---

    [Fact]
    public void GetModifiedTrainingCosts_ReturnsAllFourResourceTypes()
    {
        var warrior = CreateWarrior();
        var costs = ArmyRules.GetModifiedTrainingCosts(warrior, 1.0m);
        costs.ShouldContainKey(EResourceType.Gold);
        costs.ShouldContainKey(EResourceType.Food);
        costs.ShouldContainKey(EResourceType.Stone);
        costs.ShouldContainKey(EResourceType.Mana);
        costs.Count.ShouldBe(4);
    }

    [Fact]
    public void GetModifiedTrainingCosts_AppliesModifier()
    {
        var warrior = CreateWarrior(); // Gold=100, Food=50
        var costs = ArmyRules.GetModifiedTrainingCosts(warrior, 0.85m);
        costs[EResourceType.Gold].ShouldBe(85);  // ceil(100*0.85) = 85
        costs[EResourceType.Food].ShouldBe(43);  // ceil(50*0.85) = ceil(42.5) = 43
    }

    [Fact]
    public void GetModifiedTrainingCosts_ZeroCost_ReturnsZero()
    {
        var warrior = CreateWarrior(); // Stone=0, Mana=0
        var costs = ArmyRules.GetModifiedTrainingCosts(warrior, 1.15m);
        costs[EResourceType.Stone].ShouldBe(0);
        costs[EResourceType.Mana].ShouldBe(0);
    }
}
