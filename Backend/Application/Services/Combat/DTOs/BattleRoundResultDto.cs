namespace Application.Services.Combat.DTOs;

public class BattleRoundResultDto
{
    public int RoundNumber { get; set; }
    public Guid AttackerArmyId { get; set; }
    public Guid DefenderArmyId { get; set; }
    public string InitiativeWinner { get; set; } = string.Empty;
    public decimal AttackerInitiativeChance { get; set; }
    public decimal DefenderInitiativeChance { get; set; }
    public int DamageDealt { get; set; }
    public int ChipDamageDealt { get; set; }
    public int AttackerArmyHPAfter { get; set; }
    public int DefenderArmyHPAfter { get; set; }
    public Guid? ArmyDestroyedId { get; set; }
}
