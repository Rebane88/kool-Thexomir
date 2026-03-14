using System.ComponentModel.DataAnnotations.Schema;
using Base;

namespace Domain.Factions;

public class FactionType : BaseEntity
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new();

    public decimal BuildingCostModifier { get; set; } // e.g. 1.1 = +10% cost, 0.8 = -20%

    // Starting resources
    public int StartingGold { get; set; }
    public int StartingFood { get; set; }
    public int StartingWood { get; set; }
    public int StartingStone { get; set; }
    public int StartingMana { get; set; }

    public string? Description { get; set; }

    // Navigation
    public ICollection<FactionResourceBonus>? ResourceBonuses { get; set; }
    public ICollection<FactionUnitBonus>? UnitBonuses { get; set; }
    public ICollection<Domain.Game.Kingdom>? Kingdoms { get; set; }
}
