namespace Application.Services.Combat.DTOs.V1;

public class SelectArmiesRequest
{
    public Guid DeclaredAttackId { get; set; }
    public List<Guid> ArmyIds { get; set; } = [];
}
