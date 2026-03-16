using Domain.Map;

namespace Domain.Game;

public interface IWinConditionChecker
{
    WinCheckResult? Check(Game game, IReadOnlyList<Kingdom> kingdoms, IReadOnlyList<Tile> tiles);
}
