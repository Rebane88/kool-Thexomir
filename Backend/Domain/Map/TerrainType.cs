using System.ComponentModel.DataAnnotations.Schema;
using Base;

namespace Domain.Map;

public class TerrainType : BaseEntity, IHasName
{
    public static readonly Guid PlainsId = new("AAAAAAAA-0001-0000-0000-000000000001");

    /// <summary>
    /// Stable, culture-independent slug used as the asset-lookup identity
    /// (e.g. "plains", "forest", "magic-grove"). Never translated, never renamed.
    /// Frontend asset maps key on this, not on Name.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new();

    public decimal ResourceMultiplier { get; set; } = 1.10m;
    public ETerrainResourceBonus ResourceBonusType { get; set; }
    public string MapColor { get; set; } = string.Empty;
    public string? IconUrl { get; set; }

    // Navigation
    public ICollection<Tile>? Tiles { get; set; }
}
