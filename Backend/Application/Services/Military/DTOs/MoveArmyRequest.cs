namespace Application.Services.Military.DTOs;

public class MoveArmyRequest
{
    public Guid ArmyId { get; set; }
    public Guid TargetTileId { get; set; }
}
