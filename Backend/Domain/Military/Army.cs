using Base;
using Domain.Buildings;
using Domain.Game;

namespace Domain.Military;

public class Army : BaseEntity
{
    public Guid ArmyTypeId { get; set; }
    public Guid KingdomId { get; set; }
    public Guid BuildingId { get; set; }
    public int CurrentHP { get; set; }
    public int MaxHP { get; set; }
    public int CreatedOnRound { get; set; }

    // Navigation
    public ArmyType? ArmyType { get; set; }
    public Kingdom? Kingdom { get; set; }
    public Building? Building { get; set; }
}
