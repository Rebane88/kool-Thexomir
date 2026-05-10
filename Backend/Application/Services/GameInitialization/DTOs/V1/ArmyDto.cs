namespace Application.Services.GameInitialization.DTOs.V1;

public class ArmyDto
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public Guid KingdomId { get; set; }
    public Guid ArmyTypeId { get; set; }
    public int CurrentHP { get; set; }
    public int MaxHP { get; set; }
}
