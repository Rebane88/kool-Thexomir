namespace Application.Services.Combat.DTOs;

public class BattleSetupDto
{
    public Guid DeclaredAttackId { get; set; }
    public Guid KingdomId { get; set; }
    public int ArmiesSelected { get; set; }
    public int MaxArmies { get; set; }
}
