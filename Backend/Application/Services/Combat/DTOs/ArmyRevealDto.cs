namespace Application.Services.Combat.DTOs;

public class ArmyRevealDto
{
    public Guid DeclaredAttackId { get; set; }
    public Guid AttackerKingdomId { get; set; }
    public Guid DefenderKingdomId { get; set; }
    public List<RevealedArmyDto> AttackerArmies { get; set; } = [];
    public List<RevealedArmyDto> DefenderArmies { get; set; } = [];
}

public class RevealedArmyDto
{
    public Guid ArmyId { get; set; }
    public Guid ArmyTypeId { get; set; }
    public string ArmyTypeName { get; set; } = string.Empty;
    public int CurrentHP { get; set; }
    public int MaxHP { get; set; }
    public int Attack { get; set; }
    public int Initiative { get; set; }
}
