using Base;
using Base.Contracts;
using Domain.Map;

namespace Infrastructure.Seeding.Seeders;

public class TerrainTypeSeeder : ISeeder
{
    public int Order => 1;

    public static readonly Guid PlainsId = new("AAAAAAAA-0001-0000-0000-000000000001");
    public static readonly Guid ForestId = new("AAAAAAAA-0001-0000-0000-000000000002");
    public static readonly Guid MountainId = new("AAAAAAAA-0001-0000-0000-000000000003");
    public static readonly Guid DesertId = new("AAAAAAAA-0001-0000-0000-000000000004");
    public static readonly Guid MagicGroveId = new("AAAAAAAA-0001-0000-0000-000000000005");

    public void Seed(object context)
    {
        var db = (AppDbContext)context;
        if (db.TerrainTypes.Any()) return;

        var now = DateTime.UtcNow;

        db.TerrainTypes.AddRange(
            new TerrainType
            {
                Id = PlainsId,
                Name = new LangStr("Plains"),
                ResourceMultiplier = 1.10m,
                ResourceBonusType = ETerrainResourceBonus.Food,
                MapColor = "#90EE90",
                CreatedAt = now, UpdatedAt = now
            },
            new TerrainType
            {
                Id = ForestId,
                Name = new LangStr("Forest"),
                ResourceMultiplier = 1.10m,
                ResourceBonusType = ETerrainResourceBonus.Wood,
                MapColor = "#228B22",
                CreatedAt = now, UpdatedAt = now
            },
            new TerrainType
            {
                Id = MountainId,
                Name = new LangStr("Mountain"),
                ResourceMultiplier = 1.10m,
                ResourceBonusType = ETerrainResourceBonus.Stone,
                MapColor = "#808080",
                CreatedAt = now, UpdatedAt = now
            },
            new TerrainType
            {
                Id = DesertId,
                Name = new LangStr("Desert"),
                ResourceMultiplier = 1.10m,
                ResourceBonusType = ETerrainResourceBonus.Gold,
                MapColor = "#C2B280",
                CreatedAt = now, UpdatedAt = now
            },
            new TerrainType
            {
                Id = MagicGroveId,
                Name = new LangStr("Magic Grove"),
                ResourceMultiplier = 1.10m,
                ResourceBonusType = ETerrainResourceBonus.Mana,
                MapColor = "#9B59B6",
                CreatedAt = now, UpdatedAt = now
            }
        );

        db.SaveChanges();
    }
}
