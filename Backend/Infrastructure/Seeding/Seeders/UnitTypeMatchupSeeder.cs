using Base.Contracts;
using Domain.Military;

namespace Infrastructure.Seeding.Seeders;

public class UnitTypeMatchupSeeder : ISeeder
{
    public int Order => 4;

    public void Seed(object context)
    {
        var db = (AppDbContext)context;
        if (db.UnitTypeMatchups.Any()) return;

        var now = DateTime.UtcNow;

        // Reference unit GUIDs from UnitTypeSeeder
        var sw = UnitTypeSeeder.SwordsmanId;
        var ar = UnitTypeSeeder.ArcherId;
        var kn = UnitTypeSeeder.KnightId;
        var mg = UnitTypeSeeder.MageId;
        var ca = UnitTypeSeeder.CatapultId;

        // Matchup matrix (Attacker rows, Defender columns):
        // Attacker\Defender  Swordsman  Archer  Knight  Mage  Catapult
        // Swordsman            1.0      1.25    0.75   0.75   1.0
        // Archer               0.75     1.0     1.25   0.75   1.0
        // Knight               1.25     0.75    1.0    1.25   1.0
        // Mage                 1.25     0.75    0.75   1.0    1.0
        // Catapult             0.75     0.75    0.75   0.75   1.0

        var matchups = new (Guid attackerId, Guid defenderId, decimal multiplier, int row)[]
        {
            (sw, sw, 1.00m, 1),
            (sw, ar, 1.25m, 2),
            (sw, kn, 0.75m, 3),
            (sw, mg, 0.75m, 4),
            (sw, ca, 1.00m, 5),

            (ar, sw, 0.75m, 6),
            (ar, ar, 1.00m, 7),
            (ar, kn, 1.25m, 8),
            (ar, mg, 0.75m, 9),
            (ar, ca, 1.00m, 10),

            (kn, sw, 1.25m, 11),
            (kn, ar, 0.75m, 12),
            (kn, kn, 1.00m, 13),
            (kn, mg, 1.25m, 14),
            (kn, ca, 1.00m, 15),

            (mg, sw, 1.25m, 16),
            (mg, ar, 0.75m, 17),
            (mg, kn, 0.75m, 18),
            (mg, mg, 1.00m, 19),
            (mg, ca, 1.00m, 20),

            (ca, sw, 0.75m, 21),
            (ca, ar, 0.75m, 22),
            (ca, kn, 0.75m, 23),
            (ca, mg, 0.75m, 24),
            (ca, ca, 1.00m, 25),
        };

        foreach (var (attackerId, defenderId, multiplier, row) in matchups)
        {
            db.UnitTypeMatchups.Add(new UnitTypeMatchup
            {
                Id = new Guid($"DDDDDDDD-0001-0000-0000-{row:X12}"),
                AttackerTypeId = attackerId,
                DefenderTypeId = defenderId,
                Multiplier = multiplier,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        db.SaveChanges();
    }
}
