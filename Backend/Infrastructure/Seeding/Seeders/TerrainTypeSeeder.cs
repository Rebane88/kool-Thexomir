using Base;
using Base.Contracts;
using Domain.Map;

namespace Infrastructure.Seeding.Seeders;

public class TerrainTypeSeeder : ISeeder
{
    public int Order => 1;

    public void Seed(object context)
    {
        var db = (AppDbContext)context;
        if (db.TerrainTypes.Any()) return;

        var now = DateTime.UtcNow;

        db.TerrainTypes.AddRange(
            new TerrainType
            {
                Id = new Guid("AAAAAAAA-0001-0000-0000-000000000001"),
                Name = new LangStr("Plains"),
                DefenseBonus = 0.0m,
                MovementCost = 1,
                ResourceBonusType = TerrainResourceBonus.Food,
                CreatedAt = now,
                UpdatedAt = now
            },
            new TerrainType
            {
                Id = new Guid("AAAAAAAA-0001-0000-0000-000000000002"),
                Name = new LangStr("Forest"),
                DefenseBonus = 0.20m,
                MovementCost = 2,
                ResourceBonusType = TerrainResourceBonus.Wood,
                CreatedAt = now,
                UpdatedAt = now
            },
            new TerrainType
            {
                Id = new Guid("AAAAAAAA-0001-0000-0000-000000000003"),
                Name = new LangStr("Mountain"),
                DefenseBonus = 0.40m,
                MovementCost = 3,
                ResourceBonusType = TerrainResourceBonus.Stone,
                CreatedAt = now,
                UpdatedAt = now
            },
            new TerrainType
            {
                Id = new Guid("AAAAAAAA-0001-0000-0000-000000000004"),
                Name = new LangStr("River"),
                DefenseBonus = 0.10m,
                MovementCost = 2,
                ResourceBonusType = TerrainResourceBonus.Gold,
                CreatedAt = now,
                UpdatedAt = now
            },
            new TerrainType
            {
                Id = new Guid("AAAAAAAA-0001-0000-0000-000000000005"),
                Name = new LangStr("Magic Grove"),
                DefenseBonus = 0.10m,
                MovementCost = 1,
                ResourceBonusType = TerrainResourceBonus.Mana,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        db.SaveChanges();
    }
}
