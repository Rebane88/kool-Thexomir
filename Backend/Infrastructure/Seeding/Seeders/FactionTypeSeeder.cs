using Base;
using Base.Contracts;
using Domain.Factions;

namespace Infrastructure.Seeding.Seeders;

public class FactionTypeSeeder : ISeeder
{
    public int Order => 5;

    // Public GUIDs for cross-reference by FactionResourceBonusSeeder and FactionUnitBonusSeeder
    public static readonly Guid IronThroneId       = new Guid("EEEEEEEE-0001-0000-0000-000000000001");
    public static readonly Guid MageCouncilId      = new Guid("EEEEEEEE-0001-0000-0000-000000000002");
    public static readonly Guid MerchantRepublicId = new Guid("EEEEEEEE-0001-0000-0000-000000000003");
    public static readonly Guid ForestElvesId      = new Guid("EEEEEEEE-0001-0000-0000-000000000004");

    public void Seed(object context)
    {
        var db = (AppDbContext)context;
        if (db.FactionTypes.Any()) return;

        var now = DateTime.UtcNow;

        db.FactionTypes.AddRange(
            new FactionType
            {
                Id = IronThroneId,
                Name = new LangStr("Iron Throne"),
                BuildingCostModifier = 1.10m,
                StartingGold = 50,
                StartingFood = 0,
                StartingWood = 0,
                StartingStone = 0,
                StartingMana = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            new FactionType
            {
                Id = MageCouncilId,
                Name = new LangStr("Mage Council"),
                BuildingCostModifier = 0.90m,
                StartingGold = 0,
                StartingFood = 0,
                StartingWood = 0,
                StartingStone = 0,
                StartingMana = 30,
                CreatedAt = now,
                UpdatedAt = now
            },
            new FactionType
            {
                Id = MerchantRepublicId,
                Name = new LangStr("Merchant Republic"),
                BuildingCostModifier = 0.80m,
                StartingGold = 100,
                StartingFood = 0,
                StartingWood = 0,
                StartingStone = 0,
                StartingMana = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            new FactionType
            {
                Id = ForestElvesId,
                Name = new LangStr("Forest Elves"),
                BuildingCostModifier = 0.90m,
                StartingGold = 0,
                StartingFood = 0,
                StartingWood = 50,
                StartingStone = 0,
                StartingMana = 0,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        db.SaveChanges();
    }
}
