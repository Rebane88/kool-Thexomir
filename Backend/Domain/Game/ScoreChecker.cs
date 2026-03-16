using Domain.Buildings;
using Domain.Map;
using Domain.Military;

namespace Domain.Game;

public class ScoreChecker : IWinConditionChecker
{
    private const int TilePoints = 1;
    private const int BuildingTierMultiplier = 2;
    private const int ArmyUnitPoints = 1;

    public WinCheckResult? Check(Game game, IReadOnlyList<Kingdom> kingdoms, IReadOnlyList<Tile> tiles)
    {
        if (game.WinCondition != EWinCondition.Score) return null;
        if (!game.MaxTurnCount.HasValue) return null;
        if (game.TurnNumber <= game.MaxTurnCount.Value) return null;

        var scores = kingdoms
            .Where(k => !k.IsEliminated)
            .Select(k => (Kingdom: k, Score: CalculateScore(k, tiles)))
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => tiles.Count(t => t.KingdomId == x.Kingdom.Id))
            .ThenBy(x => x.Kingdom.CreatedAt)
            .ThenBy(x => x.Kingdom.Id)
            .ToList();

        var winner = scores.FirstOrDefault();
        return new WinCheckResult(winner.Kingdom?.Id, EWinCondition.Score, GameOver: true);
    }

    public static int CalculateScore(Kingdom kingdom, IReadOnlyList<Tile> tiles)
    {
        var kingdomTiles = tiles.Where(t => t.KingdomId == kingdom.Id).ToList();
        int tileScore = kingdomTiles.Count * TilePoints;
        int buildingScore = kingdomTiles
            .SelectMany(t => t.Buildings ?? Enumerable.Empty<Building>())
            .Sum(b => (b.BuildingType?.Tier ?? 1) * BuildingTierMultiplier);
        int armyScore = kingdom.Armies?
            .SelectMany(a => a.Units ?? Enumerable.Empty<Unit>())
            .Sum(u => u.Quantity * ArmyUnitPoints) ?? 0;
        return tileScore + buildingScore + armyScore;
    }
}
