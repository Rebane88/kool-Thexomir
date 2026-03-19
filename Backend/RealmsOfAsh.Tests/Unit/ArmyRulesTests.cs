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

    // --- CalculateHealing ---

    [Fact]
    public void CalculateHealing_BasicHealing_ReturnsCorrectHP()
    {
        // 80 + (int)(100 * 0.10 * 1.0) = 80 + 10 = 90
        ArmyRules.CalculateHealing(80, 100, 0.10m, 1.0m).ShouldBe(90);
    }

    [Fact]
    public void CalculateHealing_CapsAtMaxHP()
    {
        // 95 + 10 = 105, capped at 100
        ArmyRules.CalculateHealing(95, 100, 0.10m, 1.0m).ShouldBe(100);
    }

    [Fact]
    public void CalculateHealing_AlreadyFull_ReturnsMaxHP()
    {
        ArmyRules.CalculateHealing(100, 100, 0.10m, 1.0m).ShouldBe(100);
    }

    [Fact]
    public void CalculateHealing_HighFactionHealRate_ReturnsCorrectHP()
    {
        // 80 + (int)(100 * 0.10 * 1.50) = 80 + (int)(15.0) = 80 + 15 = 95
        ArmyRules.CalculateHealing(80, 100, 0.10m, 1.50m).ShouldBe(95);
    }

    [Fact]
    public void CalculateHealing_LowFactionHealRate_ReturnsCorrectHP()
    {
        // 80 + (int)(100 * 0.10 * 0.50) = 80 + (int)(5.0) = 80 + 5 = 85
        ArmyRules.CalculateHealing(80, 100, 0.10m, 0.50m).ShouldBe(85);
    }

    // --- CalculateTotalUpkeep ---

    private static (Army Army, ArmyType ArmyType) CreateArmyWithUpkeep(int gold, int food, int mana)
    {
        var armyType = CreateWarrior();
        armyType.UpkeepGold = gold;
        armyType.UpkeepFood = food;
        armyType.UpkeepMana = mana;
        var army = new Army
        {
            Id = Guid.NewGuid(),
            ArmyTypeId = armyType.Id,
            CurrentHP = 100,
            MaxHP = 100,
        };
        return (army, armyType);
    }

    private static List<KingdomResource> CreateResources(int gold, int food, int mana) =>
    [
        new() { KingdomId = Guid.NewGuid(), ResourceType = EResourceType.Gold, Amount = gold },
        new() { KingdomId = Guid.NewGuid(), ResourceType = EResourceType.Food, Amount = food },
        new() { KingdomId = Guid.NewGuid(), ResourceType = EResourceType.Mana, Amount = mana },
    ];

    [Fact]
    public void CalculateTotalUpkeep_EmptyList_ReturnsAllZeros()
    {
        var result = ArmyRules.CalculateTotalUpkeep([]);
        result[EResourceType.Gold].ShouldBe(0);
        result[EResourceType.Food].ShouldBe(0);
        result[EResourceType.Mana].ShouldBe(0);
    }

    [Fact]
    public void CalculateTotalUpkeep_TwoArmies_SumsCorrectly()
    {
        var a1 = CreateArmyWithUpkeep(5, 3, 0);
        var a2 = CreateArmyWithUpkeep(5, 3, 0);
        var result = ArmyRules.CalculateTotalUpkeep([a1, a2]);
        result[EResourceType.Gold].ShouldBe(10);
        result[EResourceType.Food].ShouldBe(6);
        result[EResourceType.Mana].ShouldBe(0);
    }

    [Fact]
    public void CalculateTotalUpkeep_MixedUpkeep_SumsCorrectly()
    {
        var a1 = CreateArmyWithUpkeep(5, 3, 2);
        var a2 = CreateArmyWithUpkeep(10, 5, 0);
        var result = ArmyRules.CalculateTotalUpkeep([a1, a2]);
        result[EResourceType.Gold].ShouldBe(15);
        result[EResourceType.Food].ShouldBe(8);
        result[EResourceType.Mana].ShouldBe(2);
    }

    // --- GetArmiesToDisband ---

    [Fact]
    public void GetArmiesToDisband_AllAffordable_ReturnsEmptyList()
    {
        var a1 = CreateArmyWithUpkeep(5, 3, 0);
        var resources = CreateResources(100, 100, 100);
        var result = ArmyRules.GetArmiesToDisband([a1], resources);
        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetArmiesToDisband_MostExpensiveFirst()
    {
        var cheap = CreateArmyWithUpkeep(2, 1, 0);   // total = 3
        var expensive = CreateArmyWithUpkeep(10, 5, 5); // total = 20
        // Resources can afford cheap (2+1+0=3) but not both (12+6+5=23)
        var resources = CreateResources(5, 5, 5);
        var result = ArmyRules.GetArmiesToDisband([cheap, expensive], resources);
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(expensive.Army.Id);
    }

    [Fact]
    public void GetArmiesToDisband_StopsWhenAffordable()
    {
        var a1 = CreateArmyWithUpkeep(5, 3, 0);   // total = 8
        var a2 = CreateArmyWithUpkeep(10, 5, 5);  // total = 20
        var a3 = CreateArmyWithUpkeep(3, 2, 0);   // total = 5
        // Resources: gold=10, food=10, mana=5
        // Total upkeep: 18, 10, 5 -- too much
        // Remove a2 (most expensive, 20): remaining = a1(5,3,0) + a3(3,2,0) = 8,5,0 -- affordable
        var resources = CreateResources(10, 10, 5);
        var result = ArmyRules.GetArmiesToDisband([a1, a2, a3], resources);
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(a2.Army.Id);
    }

    [Fact]
    public void GetArmiesToDisband_ZeroResources_ReturnsAll()
    {
        var a1 = CreateArmyWithUpkeep(5, 3, 0);
        var a2 = CreateArmyWithUpkeep(10, 5, 0);
        var resources = CreateResources(0, 0, 0);
        var result = ArmyRules.GetArmiesToDisband([a1, a2], resources);
        result.Count.ShouldBe(2);
    }

    // --- CanAffordUpkeep (tested indirectly through GetArmiesToDisband, but also directly) ---

    [Fact]
    public void CanAffordUpkeep_Sufficient_ReturnsTrue()
    {
        var a1 = CreateArmyWithUpkeep(5, 3, 0);
        var resources = CreateResources(10, 10, 10);
        ArmyRules.CanAffordUpkeep([a1], resources).ShouldBeTrue();
    }

    [Fact]
    public void CanAffordUpkeep_Insufficient_ReturnsFalse()
    {
        var a1 = CreateArmyWithUpkeep(15, 3, 0);
        var resources = CreateResources(10, 10, 10);
        ArmyRules.CanAffordUpkeep([a1], resources).ShouldBeFalse();
    }

    // --- ApplySituationalBonus ---

    [Fact]
    public void ApplySituationalBonus_ConditionMatches_ReturnsBonus()
    {
        var warrior = CreateWarrior();
        warrior.SituationalBonusStat = "Attack";
        warrior.SituationalBonusValue = 0.10m;
        warrior.SituationalBonusCondition = ESituationalBonusCondition.Defending;

        var result = ArmyRules.ApplySituationalBonus(warrior, isAttacker: false);
        result.ShouldNotBeNull();
        result!.Value.stat.ShouldBe("Attack");
        result.Value.multiplier.ShouldBe(1.10m);
    }

    [Fact]
    public void ApplySituationalBonus_ConditionDoesNotMatch_ReturnsNull()
    {
        var warrior = CreateWarrior();
        warrior.SituationalBonusStat = "Attack";
        warrior.SituationalBonusValue = 0.10m;
        warrior.SituationalBonusCondition = ESituationalBonusCondition.Defending;

        // Army is attacking, but bonus is for defending
        var result = ArmyRules.ApplySituationalBonus(warrior, isAttacker: true);
        result.ShouldBeNull();
    }

    [Fact]
    public void ApplySituationalBonus_NoBonusDefined_ReturnsNull()
    {
        var warrior = CreateWarrior();
        // No situational bonus fields set (all null)
        var result = ArmyRules.ApplySituationalBonus(warrior, isAttacker: true);
        result.ShouldBeNull();
    }

    // --- ApplyFactionModifiers ---

    [Fact]
    public void ApplyFactionModifiers_AppliesMultiplicatively()
    {
        var result = ArmyRules.ApplyFactionModifiers(
            baseAttack: 50, baseInitiative: 10,
            chipDmgMin: 0.05m, chipDmgMax: 0.10m,
            atkMod: 1.20m, initMod: 0.90m, chipMod: 1.10m);

        result.Attack.ShouldBe(60);       // (int)(50 * 1.20) = 60
        result.Initiative.ShouldBe(9);    // (int)(10 * 0.90) = 9
        result.ChipDamageRangeMin.ShouldBe(0.055m); // 0.05 * 1.10
        result.ChipDamageRangeMax.ShouldBe(0.110m); // 0.10 * 1.10
    }

    [Fact]
    public void ApplyFactionModifiers_NoModifiers_ReturnsOriginal()
    {
        var result = ArmyRules.ApplyFactionModifiers(
            baseAttack: 50, baseInitiative: 10,
            chipDmgMin: 0.05m, chipDmgMax: 0.10m,
            atkMod: 1.0m, initMod: 1.0m, chipMod: 1.0m);

        result.Attack.ShouldBe(50);
        result.Initiative.ShouldBe(10);
        result.ChipDamageRangeMin.ShouldBe(0.05m);
        result.ChipDamageRangeMax.ShouldBe(0.10m);
    }
}
