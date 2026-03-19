namespace Domain.Map;

public static class MapGenerator
{
    /// <summary>Returns (width, height) for the rectangular hex grid based on player count.</summary>
    public static (int width, int height) GetMapSize(int playerCount) => playerCount switch
    {
        2 => (16, 16),
        3 => (20, 20),
        4 => (24, 24),
        _ => throw new ArgumentOutOfRangeException(nameof(playerCount),
            $"Player count must be 2, 3, or 4. Got {playerCount}.")
    };

    /// <summary>
    /// Generate all axial coordinates for a rectangular hex grid using offset-to-axial conversion.
    /// Uses even-q offset layout: q = col, r = row - (col + (col &amp; 1)) / 2.
    /// </summary>
    public static List<(int q, int r)> GenerateRectangularHexGrid(int width, int height)
    {
        var tiles = new List<(int q, int r)>(width * height);
        for (int col = 0; col < width; col++)
        {
            for (int row = 0; row < height; row++)
            {
                tiles.Add(OffsetToAxial(col, row));
            }
        }
        return tiles;
    }

    /// <summary>
    /// Calculate edge-based starting positions for players.
    /// Works in offset coordinates then converts to axial.
    /// 2 players: opposite edge midpoints; 3 players: triangle; 4 players: corners.
    /// </summary>
    public static List<(int q, int r)> CalculateEdgeStartingPositions(int width, int height, int playerCount)
    {
        var offsetPositions = playerCount switch
        {
            2 => new List<(int col, int row)>
            {
                (1, height / 2),
                (width - 2, height / 2)
            },
            3 => new List<(int col, int row)>
            {
                (1, 1),
                (width - 2, 1),
                (width / 2, height - 2)
            },
            4 => new List<(int col, int row)>
            {
                (1, 1),
                (width - 2, 1),
                (1, height - 2),
                (width - 2, height - 2)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(playerCount),
                $"Player count must be 2, 3, or 4. Got {playerCount}.")
        };

        return offsetPositions.Select(p => OffsetToAxial(p.col, p.row)).ToList();
    }

    /// <summary>Convert even-q offset coordinates to axial coordinates.</summary>
    private static (int q, int r) OffsetToAxial(int col, int row)
    {
        int q = col;
        int r = row - (col + (col & 1)) / 2;
        return (q, r);
    }
}
