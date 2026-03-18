using Domain.Map;

namespace Domain.Game;

public class EliminationChecker : IWinConditionChecker
{
    public WinCheckResult? Check(Game game, IReadOnlyList<Kingdom> kingdoms, IReadOnlyList<Tile> tiles)
    {
        if (game.WinCondition != EWinCondition.Elimination) return null;

        var active = kingdoms.Where(k => k.Status != EKingdomStatus.Defeated).ToList();
        if (active.Count > 1) return null;

        var winner = active.Count == 1 ? active[0].Id : (Guid?)null;
        return new WinCheckResult(winner, EWinCondition.Elimination, GameOver: true);
    }
}
