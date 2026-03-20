using Base;

namespace Domain.Military;

public class BattleRound : BaseEntity
{
    public Guid BattleId { get; set; }
    public int RoundNumber { get; set; }

    public Guid AttackerArmyId { get; set; }
    public Guid DefenderArmyId { get; set; }

    public EBattleOutcome InitiativeWinner { get; set; }
    public decimal AttackerInitiativeChance { get; set; }
    public decimal DefenderInitiativeChance { get; set; }

    public int DamageDealt { get; set; }
    public int ChipDamageDealt { get; set; }

    public int AttackerArmyHPAfter { get; set; }
    public int DefenderArmyHPAfter { get; set; }

    public Guid? ArmyDestroyedId { get; set; }

    // Navigation
    public Battle? Battle { get; set; }
}
