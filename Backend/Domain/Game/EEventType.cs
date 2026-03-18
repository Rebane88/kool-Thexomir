namespace Domain.Game;

public enum EEventType
{
    GameStarted,
    GameEnded,
    RoundStarted,
    RoundEnded,
    TurnStarted,
    TurnEnded,
    PhaseChanged,
    ActionPointsReceived,
    BuildingConstructed,
    BuildingUpgraded,
    BuildingDestroyed,
    IncomeReceived,
    ArmyTrained,
    ArmyDestroyed,
    ArmyHealed,
    ArmyDisbanded,
    AttackDeclared,
    BattleStarted,
    BattleRoundResolved,
    BattleEnded,
    TileCaptured,
    SlotMachineSpin,
    KingdomEliminated,
    ResourcesUpdated,
    ArmySelected,
    LineupSet,
    UpkeepPaid
}
