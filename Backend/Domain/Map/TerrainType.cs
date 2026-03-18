using System.ComponentModel.DataAnnotations.Schema;
using Base;

namespace Domain.Map;

public class TerrainType : BaseEntity, IHasName
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new();

    public decimal ResourceMultiplier { get; set; } = 1.10m;
    public ETerrainResourceBonus ResourceBonusType { get; set; }
    public string MapColor { get; set; } = string.Empty;
    public string? IconUrl { get; set; }

    // Navigation
    public ICollection<Tile>? Tiles { get; set; }
}
