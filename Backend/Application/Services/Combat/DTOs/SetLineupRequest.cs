namespace Application.Services.Combat.DTOs;

public class SetLineupRequest
{
    public Guid DeclaredAttackId { get; set; }
    public List<Guid> ArmyIdsInOrder { get; set; } = [];
}
