using System.ComponentModel.DataAnnotations.Schema;
using Base;

namespace Domain.Buildings;

public class BuildingType : BaseEntity, IHasName
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new();

    public int Tier { get; set; }
    public string Chain { get; set; } = string.Empty;

    // Costs
    public int CostGold { get; set; }
    public int CostFood { get; set; }
    public int CostWood { get; set; }
    public int CostStone { get; set; }
    public int CostMana { get; set; }

    // Yields
    public int BaseYieldGold { get; set; }
    public int BaseYieldFood { get; set; }
    public int BaseYieldWood { get; set; }
    public int BaseYieldStone { get; set; }
    public int BaseYieldMana { get; set; }

    // Military
    public int ArmyCapacity { get; set; }

    [Column(TypeName = "jsonb")]
    public LangStr Description { get; set; } = new();
    public string? IconUrl { get; set; }

    // Self-referencing FK for upgrade chain
    public Guid? UnlockedByBuildingTypeId { get; set; }
    public BuildingType? UnlockedByBuildingType { get; set; }

    // Navigation
    public ICollection<Building>? Buildings { get; set; }
}
