using Base;
using Base.Contracts;
using Domain.Military;

namespace Infrastructure.Seeding.Seeders;

public class ArmyTypeSeeder : ISeeder
{
    public int Order => 4;

    public static readonly Guid WarriorId = new("CCCCCCCC-0001-0000-0000-000000000001");
    public static readonly Guid ScoutId = new("CCCCCCCC-0001-0000-0000-000000000002");
    public static readonly Guid KnightId = new("CCCCCCCC-0001-0000-0000-000000000003");
    public static readonly Guid BerserkerId = new("CCCCCCCC-0001-0000-0000-000000000004");
    public static readonly Guid MageId = new("CCCCCCCC-0001-0000-0000-000000000005");
    public static readonly Guid GuardianId = new("CCCCCCCC-0001-0000-0000-000000000006");

    public void Seed(object context)
    {
        var db = (AppDbContext)context;
        if (db.ArmyTypes.Any()) return;

        var now = DateTime.UtcNow;

        db.ArmyTypes.AddRange(
            new ArmyType
            {
                Id = WarriorId,
                Name = new LangStr("Warrior"),
                Attack = 25, HP = 100, Initiative = 50,
                DamageRangeMin = 0.60m, DamageRangeMax = 1.00m,
                ChipDamageRangeMin = 0.00m, ChipDamageRangeMax = 0.15m,
                SituationalBonusStat = "Attack",
                SituationalBonusValue = 0.10m,
                SituationalBonusCondition = ESituationalBonusCondition.Defending,
                TrainingCostGold = 40, TrainingCostFood = 10, TrainingCostStone = 0, TrainingCostMana = 0,
                UpkeepGold = 3, UpkeepFood = 2, UpkeepMana = 0,
                RequiredBuildingTypeId = BuildingTypeSeeder.BarracksId,
                CreatedAt = now, UpdatedAt = now
            },
            new ArmyType
            {
                Id = ScoutId,
                Name = new LangStr("Scout"),
                Attack = 15, HP = 60, Initiative = 70,
                DamageRangeMin = 0.50m, DamageRangeMax = 0.80m,
                ChipDamageRangeMin = 0.00m, ChipDamageRangeMax = 0.10m,
                SituationalBonusStat = null,
                SituationalBonusValue = null,
                SituationalBonusCondition = null,
                TrainingCostGold = 25, TrainingCostFood = 0, TrainingCostStone = 0, TrainingCostMana = 0,
                UpkeepGold = 2, UpkeepFood = 1, UpkeepMana = 0,
                RequiredBuildingTypeId = BuildingTypeSeeder.BarracksId,
                CreatedAt = now, UpdatedAt = now
            },
            new ArmyType
            {
                Id = KnightId,
                Name = new LangStr("Knight"),
                Attack = 35, HP = 140, Initiative = 30,
                DamageRangeMin = 0.60m, DamageRangeMax = 1.00m,
                ChipDamageRangeMin = 0.00m, ChipDamageRangeMax = 0.15m,
                SituationalBonusStat = "HP",
                SituationalBonusValue = 0.10m,
                SituationalBonusCondition = ESituationalBonusCondition.Defending,
                TrainingCostGold = 80, TrainingCostFood = 20, TrainingCostStone = 15, TrainingCostMana = 0,
                UpkeepGold = 6, UpkeepFood = 4, UpkeepMana = 0,
                RequiredBuildingTypeId = BuildingTypeSeeder.StablesId,
                CreatedAt = now, UpdatedAt = now
            },
            new ArmyType
            {
                Id = BerserkerId,
                Name = new LangStr("Berserker"),
                Attack = 35, HP = 60, Initiative = 50,
                DamageRangeMin = 0.70m, DamageRangeMax = 1.00m,
                ChipDamageRangeMin = 0.00m, ChipDamageRangeMax = 0.10m,
                SituationalBonusStat = "Attack",
                SituationalBonusValue = 0.15m,
                SituationalBonusCondition = ESituationalBonusCondition.Attacking,
                TrainingCostGold = 60, TrainingCostFood = 15, TrainingCostStone = 0, TrainingCostMana = 0,
                UpkeepGold = 4, UpkeepFood = 3, UpkeepMana = 0,
                RequiredBuildingTypeId = BuildingTypeSeeder.StablesId,
                CreatedAt = now, UpdatedAt = now
            },
            new ArmyType
            {
                Id = MageId,
                Name = new LangStr("Mage"),
                Attack = 35, HP = 60, Initiative = 70,
                DamageRangeMin = 0.40m, DamageRangeMax = 1.00m,
                ChipDamageRangeMin = 0.00m, ChipDamageRangeMax = 0.05m,
                SituationalBonusStat = "Attack",
                SituationalBonusValue = 0.20m,
                SituationalBonusCondition = ESituationalBonusCondition.Attacking,
                TrainingCostGold = 70, TrainingCostFood = 0, TrainingCostStone = 0, TrainingCostMana = 30,
                UpkeepGold = 5, UpkeepFood = 0, UpkeepMana = 2,
                RequiredBuildingTypeId = BuildingTypeSeeder.WarAcademyId,
                CreatedAt = now, UpdatedAt = now
            },
            new ArmyType
            {
                Id = GuardianId,
                Name = new LangStr("Guardian"),
                Attack = 15, HP = 140, Initiative = 30,
                DamageRangeMin = 0.50m, DamageRangeMax = 0.90m,
                ChipDamageRangeMin = 0.05m, ChipDamageRangeMax = 0.25m,
                SituationalBonusStat = "Initiative",
                SituationalBonusValue = 0.15m,
                SituationalBonusCondition = ESituationalBonusCondition.Defending,
                TrainingCostGold = 60, TrainingCostFood = 10, TrainingCostStone = 20, TrainingCostMana = 0,
                UpkeepGold = 4, UpkeepFood = 3, UpkeepMana = 0,
                RequiredBuildingTypeId = BuildingTypeSeeder.WarAcademyId,
                CreatedAt = now, UpdatedAt = now
            }
        );

        db.SaveChanges();
    }
}
