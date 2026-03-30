using System.ComponentModel.DataAnnotations.Schema;
using Base;
using Domain.Buildings;

namespace Domain.Military;

public class ArmyType : BaseEntity, IHasName
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new();

    // Combat stats
    public int Attack { get; set; }
    public int HP { get; set; }
    public int Initiative { get; set; }
    public decimal DamageRangeMin { get; set; }
    public decimal DamageRangeMax { get; set; }
    public decimal ChipDamageRangeMin { get; set; }
    public decimal ChipDamageRangeMax { get; set; }

    // Situational bonus
    public string? SituationalBonusStat { get; set; }
    public decimal? SituationalBonusValue { get; set; }
    public ESituationalBonusCondition? SituationalBonusCondition { get; set; }

    // Training costs
    public int TrainingCostGold { get; set; }
    public int TrainingCostFood { get; set; }
    public int TrainingCostStone { get; set; }
    public int TrainingCostMana { get; set; }

    // Upkeep costs (per round)
    public int UpkeepGold { get; set; }
    public int UpkeepFood { get; set; }
    public int UpkeepMana { get; set; }

    // Required building
    public Guid RequiredBuildingTypeId { get; set; }
    public BuildingType? RequiredBuildingType { get; set; }

    public string? IconUrl { get; set; }
    [Column(TypeName = "jsonb")]
    public LangStr Description { get; set; } = new();

    // Navigation
    public ICollection<Army>? Armies { get; set; }
}
