namespace Domain.Map;

public static class HexGridHelper
{
    /// <summary>Hex radius = playerCount + 2. Ensures ~30 tiles per player.</summary>
    public static int CalculateRadius(int playerCount) => playerCount + 2;

    /// <summary>Generate all axial coordinates for a hexagonal-shaped grid of given radius.</summary>
    public static List<(int q, int r)> GenerateHexGrid(int radius)
    {
        var tiles = new List<(int q, int r)>();
        for (int q = -radius; q <= radius; q++)
        {
            int r1 = Math.Max(-radius, -q - radius);
            int r2 = Math.Min(radius, -q + radius);
            for (int r = r1; r <= r2; r++)
            {
                tiles.Add((q, r));
            }
        }
        return tiles;
    }

    /// <summary>Get the 6 hex neighbors of a tile in axial coordinates.</summary>
    public static List<(int q, int r)> GetNeighbors(int q, int r)
    {
        return [(q + 1, r), (q - 1, r), (q, r + 1), (q, r - 1), (q + 1, r - 1), (q - 1, r + 1)];
    }

    /// <summary>Place kingdoms equidistantly around a ring at ~65% of map radius.</summary>
    public static List<(int q, int r)> CalculateStartingPositions(int mapRadius, int playerCount)
    {
        var positions = new List<(int q, int r)>();
        int placementRadius = (int)(mapRadius * 0.65);
        for (int i = 0; i < playerCount; i++)
        {
            double angle = 2.0 * Math.PI * i / playerCount;
            double x = placementRadius * Math.Cos(angle);
            double y = placementRadius * Math.Sin(angle);
            int qPos = (int)Math.Round(x);
            int rPos = (int)Math.Round((-x + y * Math.Sqrt(3)) / 2.0);
            positions.Add((qPos, rPos));
        }
        return positions;
    }

    /// <summary>Check if a coordinate is within the hex grid of given radius.</summary>
    public static bool IsWithinRadius((int q, int r) coord, int radius)
    {
        return Math.Max(Math.Max(Math.Abs(coord.q), Math.Abs(coord.r)), Math.Abs(coord.q + coord.r)) <= radius;
    }
}
