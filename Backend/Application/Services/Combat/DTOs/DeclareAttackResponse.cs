namespace Application.Services.Combat.DTOs;

public class DeclareAttackResponse
{
    public Guid AttackId { get; set; }
    public Guid TargetTileId { get; set; }
    public Guid RiskedTileId { get; set; }
    public Guid AttackerKingdomId { get; set; }
    public Guid DefenderKingdomId { get; set; }
    public int ActionPointsAfter { get; set; }

    // Battle readiness (included in snapshot for reconnect)
    public bool AttackerArmiesSelected { get; set; }
    public bool DefenderArmiesSelected { get; set; }
    public bool AttackerLineupConfirmed { get; set; }
    public bool DefenderLineupConfirmed { get; set; }
}
