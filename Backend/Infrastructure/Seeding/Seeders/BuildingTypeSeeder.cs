using Base;
using Base.Contracts;
using Domain.Buildings;

namespace Infrastructure.Seeding.Seeders;

public class BuildingTypeSeeder : ISeeder
{
    public int Order => 3;

    // Food chain
    private static readonly Guid FarmId       = new Guid("BBBBBBBB-0001-0000-0000-000000000001");
    private static readonly Guid WindmillId    = new Guid("BBBBBBBB-0001-0000-0000-000000000002");
    private static readonly Guid GranaryId     = new Guid("BBBBBBBB-0001-0000-0000-000000000003");

    // Wood chain
    private static readonly Guid LumberCampId  = new Guid("BBBBBBBB-0001-0000-0000-000000000004");
    private static readonly Guid SawmillId     = new Guid("BBBBBBBB-0001-0000-0000-000000000005");
    private static readonly Guid TimberHallId  = new Guid("BBBBBBBB-0001-0000-0000-000000000006");

    // Stone chain
    private static readonly Guid QuarryId      = new Guid("BBBBBBBB-0001-0000-0000-000000000007");
    private static readonly Guid MasonId       = new Guid("BBBBBBBB-0001-0000-0000-000000000008");
    private static readonly Guid StoneworksId  = new Guid("BBBBBBBB-0001-0000-0000-000000000009");

    // Gold chain
    private static readonly Guid MarketId          = new Guid("BBBBBBBB-0001-0000-0000-00000000000A");
    private static readonly Guid TradingPostId     = new Guid("BBBBBBBB-0001-0000-0000-00000000000B");
    private static readonly Guid BankId            = new Guid("BBBBBBBB-0001-0000-0000-00000000000C");

    // Mana chain
    private static readonly Guid ShrineId          = new Guid("BBBBBBBB-0001-0000-0000-00000000000D");
    private static readonly Guid WizardTowerId     = new Guid("BBBBBBBB-0001-0000-0000-00000000000E");
    private static readonly Guid ArcaneSanctumId   = new Guid("BBBBBBBB-0001-0000-0000-00000000000F");

    // Military chain
    private static readonly Guid BarracksId        = new Guid("BBBBBBBB-0001-0000-0000-000000000010");
    private static readonly Guid StablesId         = new Guid("BBBBBBBB-0001-0000-0000-000000000011");
    private static readonly Guid WarAcademyId      = new Guid("BBBBBBBB-0001-0000-0000-000000000012");

    // Defense chain
    private static readonly Guid PalisadeId        = new Guid("BBBBBBBB-0001-0000-0000-000000000013");
    private static readonly Guid StoneWallId       = new Guid("BBBBBBBB-0001-0000-0000-000000000014");
    private static readonly Guid FortressId        = new Guid("BBBBBBBB-0001-0000-0000-000000000015");

    public void Seed(object context)
    {
        var db = (AppDbContext)context;
        if (db.BuildingTypes.Any()) return;

        var now = DateTime.UtcNow;

        db.BuildingTypes.AddRange(
            // ---- Food chain ----
            new BuildingType
            {
                Id = FarmId,
                Name = new LangStr("Farm"),
                Tier = 1, Chain = "Food",
                GoldCost = 50, WoodCost = 30, StoneCost = 0, ManaCost = 0,
                FoodYield = 10, WoodYield = 0, StoneYield = 0, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = null,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = WindmillId,
                Name = new LangStr("Windmill"),
                Tier = 2, Chain = "Food",
                GoldCost = 100, WoodCost = 60, StoneCost = 0, ManaCost = 0,
                FoodYield = 20, WoodYield = 0, StoneYield = 0, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = FarmId,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = GranaryId,
                Name = new LangStr("Granary"),
                Tier = 3, Chain = "Food",
                GoldCost = 200, WoodCost = 120, StoneCost = 0, ManaCost = 0,
                FoodYield = 40, WoodYield = 0, StoneYield = 0, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = WindmillId,
                CreatedAt = now, UpdatedAt = now
            },

            // ---- Wood chain ----
            new BuildingType
            {
                Id = LumberCampId,
                Name = new LangStr("Lumber Camp"),
                Tier = 1, Chain = "Wood",
                GoldCost = 50, WoodCost = 0, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 10, StoneYield = 0, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = null,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = SawmillId,
                Name = new LangStr("Sawmill"),
                Tier = 2, Chain = "Wood",
                GoldCost = 100, WoodCost = 0, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 20, StoneYield = 0, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = LumberCampId,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = TimberHallId,
                Name = new LangStr("Timber Hall"),
                Tier = 3, Chain = "Wood",
                GoldCost = 200, WoodCost = 0, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 40, StoneYield = 0, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = SawmillId,
                CreatedAt = now, UpdatedAt = now
            },

            // ---- Stone chain ----
            new BuildingType
            {
                Id = QuarryId,
                Name = new LangStr("Quarry"),
                Tier = 1, Chain = "Stone",
                GoldCost = 50, WoodCost = 0, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 10, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = null,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = MasonId,
                Name = new LangStr("Mason"),
                Tier = 2, Chain = "Stone",
                GoldCost = 100, WoodCost = 0, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 20, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = QuarryId,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = StoneworksId,
                Name = new LangStr("Stoneworks"),
                Tier = 3, Chain = "Stone",
                GoldCost = 200, WoodCost = 0, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 40, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = MasonId,
                CreatedAt = now, UpdatedAt = now
            },

            // ---- Gold chain ----
            new BuildingType
            {
                Id = MarketId,
                Name = new LangStr("Market"),
                Tier = 1, Chain = "Gold",
                GoldCost = 50, WoodCost = 30, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 0, GoldYield = 10, ManaYield = 0,
                PrerequisiteBuildingTypeId = null,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = TradingPostId,
                Name = new LangStr("Trading Post"),
                Tier = 2, Chain = "Gold",
                GoldCost = 100, WoodCost = 60, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 0, GoldYield = 20, ManaYield = 0,
                PrerequisiteBuildingTypeId = MarketId,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = BankId,
                Name = new LangStr("Bank"),
                Tier = 3, Chain = "Gold",
                GoldCost = 200, WoodCost = 120, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 0, GoldYield = 40, ManaYield = 0,
                PrerequisiteBuildingTypeId = TradingPostId,
                CreatedAt = now, UpdatedAt = now
            },

            // ---- Mana chain ----
            new BuildingType
            {
                Id = ShrineId,
                Name = new LangStr("Shrine"),
                Tier = 1, Chain = "Mana",
                GoldCost = 50, WoodCost = 0, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 0, GoldYield = 0, ManaYield = 10,
                PrerequisiteBuildingTypeId = null,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = WizardTowerId,
                Name = new LangStr("Wizard Tower"),
                Tier = 2, Chain = "Mana",
                GoldCost = 100, WoodCost = 0, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 0, GoldYield = 0, ManaYield = 20,
                PrerequisiteBuildingTypeId = ShrineId,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = ArcaneSanctumId,
                Name = new LangStr("Arcane Sanctum"),
                Tier = 3, Chain = "Mana",
                GoldCost = 200, WoodCost = 0, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 0, GoldYield = 0, ManaYield = 40,
                PrerequisiteBuildingTypeId = WizardTowerId,
                CreatedAt = now, UpdatedAt = now
            },

            // ---- Military chain ----
            new BuildingType
            {
                Id = BarracksId,
                Name = new LangStr("Barracks"),
                Tier = 1, Chain = "Military",
                GoldCost = 50, WoodCost = 30, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 0, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = null,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = StablesId,
                Name = new LangStr("Stables"),
                Tier = 2, Chain = "Military",
                GoldCost = 100, WoodCost = 60, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 0, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = BarracksId,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = WarAcademyId,
                Name = new LangStr("War Academy"),
                Tier = 3, Chain = "Military",
                GoldCost = 200, WoodCost = 120, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 0, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = StablesId,
                CreatedAt = now, UpdatedAt = now
            },

            // ---- Defense chain ----
            new BuildingType
            {
                Id = PalisadeId,
                Name = new LangStr("Palisade"),
                Tier = 1, Chain = "Defense",
                GoldCost = 50, WoodCost = 30, StoneCost = 0, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 0, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = null,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = StoneWallId,
                Name = new LangStr("Stone Wall"),
                Tier = 2, Chain = "Defense",
                GoldCost = 100, WoodCost = 0, StoneCost = 60, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 0, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = PalisadeId,
                CreatedAt = now, UpdatedAt = now
            },
            new BuildingType
            {
                Id = FortressId,
                Name = new LangStr("Fortress"),
                Tier = 3, Chain = "Defense",
                GoldCost = 200, WoodCost = 0, StoneCost = 120, ManaCost = 0,
                FoodYield = 0, WoodYield = 0, StoneYield = 0, GoldYield = 0, ManaYield = 0,
                PrerequisiteBuildingTypeId = StoneWallId,
                CreatedAt = now, UpdatedAt = now
            }
        );

        db.SaveChanges();
    }
}
