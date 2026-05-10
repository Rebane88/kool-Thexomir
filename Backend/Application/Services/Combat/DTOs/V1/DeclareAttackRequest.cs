namespace Application.Services.Combat.DTOs.V1;

public class DeclareAttackRequest
{
    public Guid TargetTileId { get; set; }
    public Guid RiskedTileId { get; set; }
}
