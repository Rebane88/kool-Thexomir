namespace Domain.Game;

public static class TurnRules
{
    /// <summary>
    /// Returns the next phase in the cycle: Action -> Battle -> Income -> RoundEnd -> Action.
    /// </summary>
    public static EGamePhase GetNextPhase(EGamePhase currentPhase) => currentPhase switch
    {
        EGamePhase.Action => EGamePhase.Battle,
        EGamePhase.Battle => EGamePhase.Income,
        EGamePhase.Income => EGamePhase.RoundEnd,
        EGamePhase.RoundEnd => EGamePhase.Action,
        _ => throw new ArgumentOutOfRangeException(nameof(currentPhase), currentPhase, "Unknown game phase.")
    };

    /// <summary>
    /// Calculates action points as base + faction modifier, floored at 0.
    /// </summary>
    public static int CalculateActionPoints(int baseActionPoints, int factionActionPointModifier)
        => Math.Max(0, baseActionPoints + factionActionPointModifier);

    /// <summary>
    /// Validates whether a kingdom can end its turn.
    /// Returns null on success, error message on failure.
    /// </summary>
    public static string? ValidateEndTurn(EGamePhase currentPhase, Guid? currentTurnKingdomId, Guid kingdomId)
    {
        if (currentPhase != EGamePhase.Action)
            return "Can only end turn during Action Phase.";

        if (currentTurnKingdomId != kingdomId)
            return "It is not your turn.";

        return null;
    }

    /// <summary>
    /// Finds the next active kingdom by turn order after the current turn order.
    /// Returns null when all players have gone (no wrap-around -- signals phase transition).
    /// </summary>
    public static Kingdom? GetNextActiveKingdom(List<Kingdom> kingdoms, int currentTurnOrder)
        => kingdoms
            .Where(k => k.TurnOrder > currentTurnOrder && k.Status == EKingdomStatus.Active)
            .OrderBy(k => k.TurnOrder)
            .FirstOrDefault();

    /// <summary>
    /// Checks whether the turn deadline has passed.
    /// Returns false if no deadline is set.
    /// </summary>
    public static bool IsTurnExpired(DateTime? turnDeadline)
        => turnDeadline is not null && DateTime.UtcNow > turnDeadline;
}
