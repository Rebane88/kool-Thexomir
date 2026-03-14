using System.ComponentModel.DataAnnotations.Schema;
using Base;

namespace Domain.Military;

public class UnitType : BaseEntity
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new();

    public int BaseStrength { get; set; }

    // Costs
    public int GoldCost { get; set; }
    public int FoodCost { get; set; }
    public int WoodCost { get; set; }
    public int StoneCost { get; set; }
    public int ManaCost { get; set; }

    public int Upkeep { get; set; } // food per turn

    public string? Description { get; set; }
    public string? IconUrl { get; set; }

    // Navigation
    public ICollection<Unit>? Units { get; set; }
    public ICollection<UnitTypeMatchup>? AttackerMatchups { get; set; }
    public ICollection<UnitTypeMatchup>? DefenderMatchups { get; set; }
}
