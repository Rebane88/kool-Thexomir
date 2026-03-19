namespace Application.Services.Combat.DTOs;

public class DeclareAttackResponse
{
    public Guid AttackId { get; set; }
    public Guid TargetTileId { get; set; }
    public Guid RiskedTileId { get; set; }
    public Guid AttackerKingdomId { get; set; }
    public Guid DefenderKingdomId { get; set; }
}
