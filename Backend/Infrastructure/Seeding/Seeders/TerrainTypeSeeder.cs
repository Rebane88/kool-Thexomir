using Base;
using Base.Contracts;
using Domain.Map;

namespace Infrastructure.Seeding.Seeders;

public class TerrainTypeSeeder : ISeeder
{
    public int Order => 1;

    public static readonly Guid PlainsId = TerrainType.PlainsId;
    public static readonly Guid ForestId = new("AAAAAAAA-0001-0000-0000-000000000002");
    public static readonly Guid MountainId = new("AAAAAAAA-0001-0000-0000-000000000003");
    public static readonly Guid DesertId = new("AAAAAAAA-0001-0000-0000-000000000004");
    public static readonly Guid MagicGroveId = new("AAAAAAAA-0001-0000-0000-000000000005");

    private static LangStr L(string en, string et)
    {
        var ls = new LangStr(en, "en");
        ls.SetTranslation(et, "et");
        return ls;
    }

    public void Seed(object context)
    {
        var db = (AppDbContext)context;
        if (db.TerrainTypes.Any()) return;

        var now = DateTime.UtcNow;

        db.TerrainTypes.AddRange(
            new TerrainType
            {
                Id = PlainsId,
                Code = "plains",
                Name = L("Plains", "Tasandik"),
                ResourceMultiplier = 1.10m,
                ResourceBonusType = ETerrainResourceBonus.Food,
                MapColor = "#90EE90",
                CreatedAt = now, UpdatedAt = now
            },
            new TerrainType
            {
                Id = ForestId,
                Code = "forest",
                Name = L("Forest", "Mets"),
                ResourceMultiplier = 1.10m,
                ResourceBonusType = ETerrainResourceBonus.Wood,
                MapColor = "#228B22",
                CreatedAt = now, UpdatedAt = now
            },
            new TerrainType
            {
                Id = MountainId,
                Code = "mountain",
                Name = L("Mountain", "Mägi"),
                ResourceMultiplier = 1.10m,
                ResourceBonusType = ETerrainResourceBonus.Stone,
                MapColor = "#808080",
                CreatedAt = now, UpdatedAt = now
            },
            new TerrainType
            {
                Id = DesertId,
                Code = "desert",
                Name = L("Desert", "Kõrb"),
                ResourceMultiplier = 1.10m,
                ResourceBonusType = ETerrainResourceBonus.Gold,
                MapColor = "#C2B280",
                CreatedAt = now, UpdatedAt = now
            },
            new TerrainType
            {
                Id = MagicGroveId,
                Code = "magic-grove",
                Name = L("Magic Grove", "Võlusalubaar"),
                ResourceMultiplier = 1.10m,
                ResourceBonusType = ETerrainResourceBonus.Mana,
                MapColor = "#9B59B6",
                CreatedAt = now, UpdatedAt = now
            }
        );

        db.SaveChanges();
    }
}