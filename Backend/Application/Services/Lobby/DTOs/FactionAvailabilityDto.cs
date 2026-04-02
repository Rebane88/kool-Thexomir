namespace Application.Services.Lobby.DTOs;

public class FactionAvailabilityDto
{
    public Guid FactionTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string? Description { get; set; }
    public decimal AttackModifier { get; set; }
    public decimal HPModifier { get; set; }
    public decimal InitiativeModifier { get; set; }
    public decimal ChipDamageModifier { get; set; }
    public decimal ResourceProductionModifier { get; set; }
    public decimal BuildingCostModifier { get; set; }
    public decimal TrainingCostModifier { get; set; }
    public int ActionPointModifier { get; set; }
    public decimal HealRateModifier { get; set; }
}
