namespace Application.Services.Army.DTOs;

public class ArmyTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Attack { get; set; }
    public int HP { get; set; }
    public int Initiative { get; set; }
    public decimal DamageRangeMin { get; set; }
    public decimal DamageRangeMax { get; set; }
    public decimal ChipDamageRangeMin { get; set; }
    public decimal ChipDamageRangeMax { get; set; }
    public string? SituationalBonusStat { get; set; }
    public decimal? SituationalBonusValue { get; set; }
    public string? SituationalBonusCondition { get; set; }
    public int TrainingCostGold { get; set; }
    public int TrainingCostFood { get; set; }
    public int TrainingCostStone { get; set; }
    public int TrainingCostMana { get; set; }
    public int UpkeepGold { get; set; }
    public int UpkeepFood { get; set; }
    public int UpkeepMana { get; set; }
    public Guid RequiredBuildingTypeId { get; set; }
    public string? RequiredBuildingName { get; set; }
}
