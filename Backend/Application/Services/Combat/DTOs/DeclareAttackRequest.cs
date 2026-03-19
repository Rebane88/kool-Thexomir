namespace Application.Services.Combat.DTOs;

public class DeclareAttackRequest
{
    public Guid TargetTileId { get; set; }
    public Guid RiskedTileId { get; set; }
}
