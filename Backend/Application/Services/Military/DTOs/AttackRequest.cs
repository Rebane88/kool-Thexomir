namespace Application.Services.Military.DTOs;

public class AttackRequest
{
    public Guid AttackerArmyId { get; set; }
    public Guid DefenderTileId { get; set; }
}
