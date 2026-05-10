namespace Application.Services.Army.DTOs.V1;

public class ArmyTrainedDto
{
    public Guid ArmyId { get; set; }
    public Guid BuildingId { get; set; }
    public Guid ArmyTypeId { get; set; }
    public string ArmyTypeName { get; set; } = string.Empty;
    public Guid KingdomId { get; set; }
    public int CurrentHP { get; set; }
    public int MaxHP { get; set; }
    public Dictionary<string, int> ResourcesAfter { get; set; } = new();
    public int ActionPointsAfter { get; set; }
}
