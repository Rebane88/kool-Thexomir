namespace Application.Services.Combat.DTOs.V1;

public class SetLineupRequest
{
    public Guid DeclaredAttackId { get; set; }
    public List<Guid> ArmyIdsInOrder { get; set; } = [];
}
