using System.ComponentModel.DataAnnotations.Schema;
using Base;

namespace Domain.Buildings;

public class BuildingType : BaseEntity
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new();

    public int Tier { get; set; }
    public string Chain { get; set; } = string.Empty;

    // Costs
    public int GoldCost { get; set; }
    public int WoodCost { get; set; }
    public int StoneCost { get; set; }
    public int ManaCost { get; set; }

    // Yields
    public int FoodYield { get; set; }
    public int WoodYield { get; set; }
    public int StoneYield { get; set; }
    public int GoldYield { get; set; }
    public int ManaYield { get; set; }

    public string? Description { get; set; }
    public string? IconUrl { get; set; }

    // Self-referencing FK for upgrade chain
    public Guid? PrerequisiteBuildingTypeId { get; set; }
    public BuildingType? PrerequisiteBuildingType { get; set; }

    // Navigation
    public ICollection<Building>? Buildings { get; set; }
}
