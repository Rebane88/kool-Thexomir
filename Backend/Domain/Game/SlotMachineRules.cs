using System.Text.Json;

namespace Domain.Game;

public static class SlotMachineRules
{
    private static readonly int[] Outcomes = [-2, -1, 0, 1, 2];
    private static readonly int[] DefaultWeights = [5, 25, 30, 25, 15];

    /// <summary>
    /// Validates whether a kingdom can spin the slot machine.
    /// Returns null on success, error message on failure.
    /// </summary>
    public static string? ValidateSpin(EGamePhase currentPhase, decimal goldAmount, int spinCostGold)
    {
        if (currentPhase != EGamePhase.Action)
            return "Can only spin during Action Phase.";

        if (goldAmount < spinCostGold)
            return $"Insufficient Gold. Need {spinCostGold}, have {(int)goldAmount}.";

        return null;
    }

    /// <summary>
    /// Determines the slot machine outcome using weighted random selection.
    /// Outcomes are [-2, -1, 0, +1, +2] with cumulative weight algorithm.
    /// Falls back to default weights on null/empty/invalid JSON.
    /// </summary>
    public static int DetermineOutcome(string? slotOutcomeWeightsJson, Random random)
    {
        var weights = ParseWeights(slotOutcomeWeightsJson);
        var totalWeight = weights.Sum();
        var roll = random.Next(totalWeight);

        var cumulative = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
                return Outcomes[i];
        }

        return Outcomes[^1];
    }

    /// <summary>
    /// Applies slot machine outcome to current action points, floored at 0.
    /// </summary>
    public static int ApplyOutcome(int currentActionPoints, int outcome)
        => Math.Max(0, currentActionPoints + outcome);

    // --- Private helpers ---

    private static int[] ParseWeights(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return DefaultWeights;

        try
        {
            var parsed = JsonSerializer.Deserialize<int[]>(json);
            return parsed is { Length: > 0 } ? parsed : DefaultWeights;
        }
        catch
        {
            return DefaultWeights;
        }
    }
}
