namespace Application.Services.Combat.DTOs;

public class SelectArmiesRequest
{
    public Guid DeclaredAttackId { get; set; }
    public List<Guid> ArmyIds { get; set; } = [];
}
