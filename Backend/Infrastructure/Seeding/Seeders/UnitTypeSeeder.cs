using Base;
using Base.Contracts;
using Domain.Military;

namespace Infrastructure.Seeding.Seeders;

public class UnitTypeSeeder : ISeeder
{
    public int Order => 2;

    // Public GUIDs for cross-reference by UnitTypeMatchupSeeder and FactionUnitBonusSeeder
    public static readonly Guid SwordsmanId = new Guid("CCCCCCCC-0001-0000-0000-000000000001");
    public static readonly Guid ArcherId    = new Guid("CCCCCCCC-0001-0000-0000-000000000002");
    public static readonly Guid KnightId    = new Guid("CCCCCCCC-0001-0000-0000-000000000003");
    public static readonly Guid MageId      = new Guid("CCCCCCCC-0001-0000-0000-000000000004");
    public static readonly Guid CatapultId  = new Guid("CCCCCCCC-0001-0000-0000-000000000005");

    public void Seed(object context)
    {
        var db = (AppDbContext)context;
        if (db.UnitTypes.Any()) return;

        var now = DateTime.UtcNow;

        db.UnitTypes.AddRange(
            new UnitType
            {
                Id = SwordsmanId,
                Name = new LangStr("Swordsman"),
                BaseStrength = 10,
                GoldCost = 50,
                FoodCost = 10,
                WoodCost = 10,
                StoneCost = 0,
                ManaCost = 0,
                Upkeep = 2,
                CreatedAt = now,
                UpdatedAt = now
            },
            new UnitType
            {
                Id = ArcherId,
                Name = new LangStr("Archer"),
                BaseStrength = 8,
                GoldCost = 40,
                FoodCost = 10,
                WoodCost = 15,
                StoneCost = 0,
                ManaCost = 0,
                Upkeep = 2,
                CreatedAt = now,
                UpdatedAt = now
            },
            new UnitType
            {
                Id = KnightId,
                Name = new LangStr("Knight"),
                BaseStrength = 15,
                GoldCost = 80,
                FoodCost = 15,
                WoodCost = 5,
                StoneCost = 5,
                ManaCost = 0,
                Upkeep = 3,
                CreatedAt = now,
                UpdatedAt = now
            },
            new UnitType
            {
                Id = MageId,
                Name = new LangStr("Mage"),
                BaseStrength = 12,
                GoldCost = 60,
                FoodCost = 5,
                WoodCost = 0,
                StoneCost = 0,
                ManaCost = 30,
                Upkeep = 4,
                CreatedAt = now,
                UpdatedAt = now
            },
            new UnitType
            {
                Id = CatapultId,
                Name = new LangStr("Catapult"),
                BaseStrength = 20,
                GoldCost = 100,
                FoodCost = 5,
                WoodCost = 30,
                StoneCost = 20,
                ManaCost = 0,
                Upkeep = 5,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        db.SaveChanges();
    }
}
