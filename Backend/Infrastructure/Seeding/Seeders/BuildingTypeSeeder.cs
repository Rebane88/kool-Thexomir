using Base;
using Base.Contracts;
using Domain.Buildings;

namespace Infrastructure.Seeding.Seeders;

public class BuildingTypeSeeder : ISeeder
{
    public int Order => 3;

    // Castle (special)
    public static readonly Guid CastleId = BuildingType.CastleId;

    // Food chain
    public static readonly Guid FarmId = new("BBBBBBBB-0001-0000-0000-000000000001");
    public static readonly Guid WindmillId = new("BBBBBBBB-0001-0000-0000-000000000002");
    public static readonly Guid GranaryId = new("BBBBBBBB-0001-0000-0000-000000000003");

    // Wood chain
    public static readonly Guid LumberCampId = new("BBBBBBBB-0001-0000-0000-000000000004");
    public static readonly Guid SawmillId = new("BBBBBBBB-0001-0000-0000-000000000005");
    public static readonly Guid TimberHallId = new("BBBBBBBB-0001-0000-0000-000000000006");

    // Stone chain
    public static readonly Guid QuarryId = new("BBBBBBBB-0001-0000-0000-000000000007");
    public static readonly Guid MasonId = new("BBBBBBBB-0001-0000-0000-000000000008");
    public static readonly Guid StoneworksId = new("BBBBBBBB-0001-0000-0000-000000000009");

    // Gold chain
    public static readonly Guid MarketId = new("BBBBBBBB-0001-0000-0000-00000000000A");
    public static readonly Guid TradingPostId = new("BBBBBBBB-0001-0000-0000-00000000000B");
    public static readonly Guid BankId = new("BBBBBBBB-0001-0000-0000-00000000000C");

    // Mana chain
    public static readonly Guid ShrineId = new("BBBBBBBB-0001-0000-0000-00000000000D");
    public static readonly Guid WizardTowerId = new("BBBBBBBB-0001-0000-0000-00000000000E");
    public static readonly Guid ArcaneSanctumId = new("BBBBBBBB-0001-0000-0000-00000000000F");

    // Military chain
    public static readonly Guid BarracksId = new("BBBBBBBB-0001-0000-0000-000000000010");
    public static readonly Guid StablesId = new("BBBBBBBB-0001-0000-0000-000000000011");
    public static readonly Guid WarAcademyId = new("BBBBBBBB-0001-0000-0000-000000000012");

    private static LangStr L(string en, string et)
    {
        var ls = new LangStr(en, "en");
        ls.SetTranslation(et, "et");
        return ls;
    }

    public void Seed(object context)
    {
        var db = (AppDbContext)context;
        if (db.BuildingTypes.Any()) return;

        var now = DateTime.UtcNow;

        db.BuildingTypes.AddRange(
            // ---- Castle (special, tier 0) ----
            new BuildingType
            {
                Id = CastleId,
                Code = "castle",
                Name = L("Castle", "Loss"),
                Tier = 0, Chain = "Castle",
                CostGold = 0, CostFood = 0, CostWood = 0, CostStone = 0, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 10, BaseYieldWood = 10, BaseYieldStone = 10, BaseYieldMana = 0,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = null,
                CreatedAt = now, UpdatedAt = now
            },

            // ---- Food chain ----
            new BuildingType
            {
                Id = FarmId,
                Code = "farm",
                Name = L("Farm", "Talu"),
                Tier = 1, Chain = "Food",
                CostGold = 0, CostFood = 0, CostWood = 30, CostStone = 0, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 10, BaseYieldWood = 0, BaseYieldStone = 0, BaseYieldMana = 0,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = null,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = WindmillId,
                Code = "windmill",
                Name = L("Windmill", "Tuuleveski"),
                Tier = 2, Chain = "Food",
                CostGold = 60, CostFood = 0, CostWood = 30, CostStone = 0, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 20, BaseYieldWood = 0, BaseYieldStone = 0, BaseYieldMana = 0,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = FarmId,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = GranaryId,
                Code = "granary",
                Name = L("Granary", "Viljaait"),
                Tier = 3, Chain = "Food",
                CostGold = 100, CostFood = 0, CostWood = 0, CostStone = 50, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 35, BaseYieldWood = 0, BaseYieldStone = 0, BaseYieldMana = 0,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = WindmillId,
                CreatedAt = now, UpdatedAt = now
            },

            // ---- Wood chain ----
            new BuildingType
            {
                Id = LumberCampId,
                Code = "lumber-camp",
                Name = L("Lumber Camp", "Metsalaager"),
                Tier = 1, Chain = "Wood",
                CostGold = 30, CostFood = 0, CostWood = 0, CostStone = 0, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 0, BaseYieldWood = 8, BaseYieldStone = 0, BaseYieldMana = 0,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = null,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = SawmillId,
                Code = "sawmill",
                Name = L("Sawmill", "Saeveski"),
                Tier = 2, Chain = "Wood",
                CostGold = 50, CostFood = 0, CostWood = 0, CostStone = 20, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 0, BaseYieldWood = 18, BaseYieldStone = 0, BaseYieldMana = 0,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = LumberCampId,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = TimberHallId,
                Code = "timber-hall",
                Name = L("Timber Hall", "Puidukoda"),
                Tier = 3, Chain = "Wood",
                CostGold = 80, CostFood = 0, CostWood = 0, CostStone = 50, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 0, BaseYieldWood = 30, BaseYieldStone = 0, BaseYieldMana = 0,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = SawmillId,
                CreatedAt = now, UpdatedAt = now
            },

            // ---- Stone chain ----
            new BuildingType
            {
                Id = QuarryId,
                Code = "quarry",
                Name = L("Quarry", "Karjäär"),
                Tier = 1, Chain = "Stone",
                CostGold = 40, CostFood = 0, CostWood = 10, CostStone = 0, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 0, BaseYieldWood = 0, BaseYieldStone = 6, BaseYieldMana = 0,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = null,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = MasonId,
                Code = "mason",
                Name = L("Mason", "Müürsepp"),
                Tier = 2, Chain = "Stone",
                CostGold = 60, CostFood = 0, CostWood = 30, CostStone = 0, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 0, BaseYieldWood = 0, BaseYieldStone = 14, BaseYieldMana = 0,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = QuarryId,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = StoneworksId,
                Code = "stoneworks",
                Name = L("Stoneworks", "Kivitöökoda"),
                Tier = 3, Chain = "Stone",
                CostGold = 100, CostFood = 0, CostWood = 60, CostStone = 0, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 0, BaseYieldWood = 0, BaseYieldStone = 25, BaseYieldMana = 0,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = MasonId,
                CreatedAt = now, UpdatedAt = now
            },

            // ---- Gold chain ----
            new BuildingType
            {
                Id = MarketId,
                Code = "market",
                Name = L("Market", "Turg"),
                Tier = 1, Chain = "Gold",
                CostGold = 0, CostFood = 0, CostWood = 40, CostStone = 0, CostMana = 0,
                BaseYieldGold = 12, BaseYieldFood = 0, BaseYieldWood = 0, BaseYieldStone = 0, BaseYieldMana = 0,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = null,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = TradingPostId,
                Code = "trading-post",
                Name = L("Trading Post", "Kaubanduspost"),
                Tier = 2, Chain = "Gold",
                CostGold = 0, CostFood = 0, CostWood = 80, CostStone = 30, CostMana = 0,
                BaseYieldGold = 25, BaseYieldFood = 0, BaseYieldWood = 0, BaseYieldStone = 0, BaseYieldMana = 0,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = MarketId,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = BankId,
                Code = "bank",
                Name = L("Bank", "Pank"),
                Tier = 3, Chain = "Gold",
                CostGold = 0, CostFood = 0, CostWood = 120, CostStone = 60, CostMana = 0,
                BaseYieldGold = 45, BaseYieldFood = 0, BaseYieldWood = 0, BaseYieldStone = 0, BaseYieldMana = 0,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = TradingPostId,
                CreatedAt = now, UpdatedAt = now
            },

            // ---- Mana chain ----
            new BuildingType
            {
                Id = ShrineId,
                Code = "shrine",
                Name = L("Shrine", "Pühamu"),
                Tier = 1, Chain = "Mana",
                CostGold = 50, CostFood = 0, CostWood = 20, CostStone = 0, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 0, BaseYieldWood = 0, BaseYieldStone = 0, BaseYieldMana = 5,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = null,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = WizardTowerId,
                Code = "wizard-tower",
                Name = L("Wizard Tower", "Võluri torn"),
                Tier = 2, Chain = "Mana",
                CostGold = 80, CostFood = 0, CostWood = 0, CostStone = 40, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 0, BaseYieldWood = 0, BaseYieldStone = 0, BaseYieldMana = 12,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = ShrineId,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = ArcaneSanctumId,
                Code = "arcane-sanctum",
                Name = L("Arcane Sanctum", "Müstiline pühamu"),
                Tier = 3, Chain = "Mana",
                CostGold = 150, CostFood = 0, CostWood = 0, CostStone = 80, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 0, BaseYieldWood = 0, BaseYieldStone = 0, BaseYieldMana = 22,
                ArmyCapacity = 0,
                UnlockedByBuildingTypeId = WizardTowerId,
                CreatedAt = now, UpdatedAt = now
            },

            // ---- Military chain ----
            new BuildingType
            {
                Id = BarracksId,
                Code = "barracks",
                Name = L("Barracks", "Kasarmud"),
                Tier = 1, Chain = "Military",
                CostGold = 50, CostFood = 0, CostWood = 30, CostStone = 0, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 0, BaseYieldWood = 0, BaseYieldStone = 0, BaseYieldMana = 0,
                ArmyCapacity = 3,
                UnlockedByBuildingTypeId = null,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = StablesId,
                Code = "stables",
                Name = L("Stables", "Tallid"),
                Tier = 2, Chain = "Military",
                CostGold = 100, CostFood = 0, CostWood = 50, CostStone = 20, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 0, BaseYieldWood = 0, BaseYieldStone = 0, BaseYieldMana = 0,
                ArmyCapacity = 3,
                UnlockedByBuildingTypeId = BarracksId,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = WarAcademyId,
                Code = "war-academy",
                Name = L("War Academy", "Sõjaakadeemia"),
                Tier = 3, Chain = "Military",
                CostGold = 200, CostFood = 0, CostWood = 80, CostStone = 50, CostMana = 0,
                BaseYieldGold = 0, BaseYieldFood = 0, BaseYieldWood = 0, BaseYieldStone = 0, BaseYieldMana = 0,
                ArmyCapacity = 3,
                UnlockedByBuildingTypeId = StablesId,
                CreatedAt = now, UpdatedAt = now
            }
        );

        db.SaveChanges();
    }
}
