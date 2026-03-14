using Base.Contracts;
using Domain.Factions;

namespace Infrastructure.Seeding.Seeders;

public class FactionUnitBonusSeeder : ISeeder
{
    public int Order => 7;

    public void Seed(object context)
    {
        var db = (AppDbContext)context;
        if (db.FactionUnitBonuses.Any()) return;

        var now = DateTime.UtcNow;

        // Iron Throne: +20% all units (UnitTypeId = null)
        // Mage Council: +20% Mages only
        // Merchant Republic: -10% all units (UnitTypeId = null, Multiplier = 0.9)
        // Forest Elves: no unit bonuses (0 rows)

        db.FactionUnitBonuses.AddRange(
            new FactionUnitBonus
            {
                Id = new Guid("11111111-0001-0000-0000-000000000001"),
                FactionTypeId = FactionTypeSeeder.IronThroneId,
                UnitTypeId = null,
                Multiplier = 1.2m,
                CreatedAt = now,
                UpdatedAt = now
            },
            new FactionUnitBonus
            {
                Id = new Guid("11111111-0001-0000-0000-000000000002"),
                FactionTypeId = FactionTypeSeeder.MageCouncilId,
                UnitTypeId = UnitTypeSeeder.MageId,
                Multiplier = 1.2m,
                CreatedAt = now,
                UpdatedAt = now
            },
            new FactionUnitBonus
            {
                Id = new Guid("11111111-0001-0000-0000-000000000003"),
                FactionTypeId = FactionTypeSeeder.MerchantRepublicId,
                UnitTypeId = null,
                Multiplier = 0.9m,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        db.SaveChanges();
    }
}
