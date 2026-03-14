using Base.Contracts;
using Domain.Factions;
using Domain.Resources;

namespace Infrastructure.Seeding.Seeders;

public class FactionResourceBonusSeeder : ISeeder
{
    public int Order => 6;

    public void Seed(object context)
    {
        var db = (AppDbContext)context;
        if (db.FactionResourceBonuses.Any()) return;

        var now = DateTime.UtcNow;

        // Iron Throne: no resource bonuses (0 rows)
        // Mage Council: Mana +20%
        // Merchant Republic: Gold +30%
        // Forest Elves: Food +20%, Wood +20%

        db.FactionResourceBonuses.AddRange(
            new FactionResourceBonus
            {
                Id = new Guid("FFFFFFFF-0001-0000-0000-000000000001"),
                FactionTypeId = FactionTypeSeeder.MageCouncilId,
                ResourceType = ResourceType.Mana,
                Multiplier = 1.2m,
                CreatedAt = now,
                UpdatedAt = now
            },
            new FactionResourceBonus
            {
                Id = new Guid("FFFFFFFF-0001-0000-0000-000000000002"),
                FactionTypeId = FactionTypeSeeder.MerchantRepublicId,
                ResourceType = ResourceType.Gold,
                Multiplier = 1.3m,
                CreatedAt = now,
                UpdatedAt = now
            },
            new FactionResourceBonus
            {
                Id = new Guid("FFFFFFFF-0001-0000-0000-000000000003"),
                FactionTypeId = FactionTypeSeeder.ForestElvesId,
                ResourceType = ResourceType.Food,
                Multiplier = 1.2m,
                CreatedAt = now,
                UpdatedAt = now
            },
            new FactionResourceBonus
            {
                Id = new Guid("FFFFFFFF-0001-0000-0000-000000000004"),
                FactionTypeId = FactionTypeSeeder.ForestElvesId,
                ResourceType = ResourceType.Wood,
                Multiplier = 1.2m,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        db.SaveChanges();
    }
}
