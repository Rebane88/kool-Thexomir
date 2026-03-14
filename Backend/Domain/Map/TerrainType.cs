using System.ComponentModel.DataAnnotations.Schema;
using Base;

namespace Domain.Map;

public class TerrainType : BaseEntity
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new();

    public decimal DefenseBonus { get; set; }
    public int MovementCost { get; set; }
    public ETerrainResourceBonus ResourceBonusType { get; set; }

    // Navigation
    public ICollection<Tile>? Tiles { get; set; }
}
