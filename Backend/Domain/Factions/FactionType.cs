using System.ComponentModel.DataAnnotations.Schema;
using Base;
using Domain.Resources;

namespace Domain.Factions;

public class FactionType : BaseEntity, IHasName
{
    [Column(TypeName = "jsonb")]
    public LangStr Name { get; set; } = new();

    public string? Description { get; set; }
    public string? Lore { get; set; }

    // Combat modifiers (multiplicative)
    public decimal AttackModifier { get; set; } = 1.0m;
    public decimal HPModifier { get; set; } = 1.0m;
    public decimal InitiativeModifier { get; set; } = 1.0m;
    public decimal ChipDamageModifier { get; set; } = 1.0m;

    // Economy modifiers (multiplicative)
    public decimal ResourceProductionModifier { get; set; } = 1.0m;
    public decimal BuildingCostModifier { get; set; } = 1.0m;
    public decimal TrainingCostModifier { get; set; } = 1.0m;

    // Action system (additive)
    public int ActionPointModifier { get; set; }

    // Recovery (multiplicative)
    public decimal HealRateModifier { get; set; } = 1.0m;

    // Starting bonus
    public EResourceType? StartingBonusResource { get; set; }
    public int StartingBonusAmount { get; set; }

    public string? IconUrl { get; set; }

    // Navigation
    public ICollection<Domain.Game.Kingdom>? Kingdoms { get; set; }
}
