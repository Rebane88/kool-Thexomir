using API.Areas.Public.ViewModels;
using Application.Services.GameInitialization.DTOs;

namespace API.Areas.Public.Helpers;

/// <summary>
/// Pure C# port of the pointy-top hex math from Frontend/src/features/game/canvas/hex-math.ts.
/// Reference: redblobgames.com/grids/hexagons
/// </summary>
public static class HexLayout
{
    public record Point(double X, double Y);

    /// <summary>
    /// Convert axial hex coordinates to pixel position (pointy-top layout).
    /// Matches: axialToPixel from hex-math.ts (origin at 0,0).
    /// </summary>
    public static (double X, double Y) AxialToPixel(int q, int r, double size)
    {
        var x = size * (Math.Sqrt(3) * q + Math.Sqrt(3) / 2.0 * r);
        var y = size * (1.5 * r);
        return (x, y);
    }

    /// <summary>
    /// Calculate the 6 corner vertices of a pointy-top hex centered at (cx, cy).
    /// First corner at 30 degrees, then every 60 degrees.
    /// Matches: hexCorners from hex-math.ts.
    /// </summary>
    public static List<Point> HexCorners(double cx, double cy, double size)
    {
        var corners = new List<Point>(6);
        for (int i = 0; i < 6; i++)
        {
            var angle = Math.PI / 180.0 * (30 + 60 * i);
            corners.Add(new Point(cx + size * Math.Cos(angle), cy + size * Math.Sin(angle)));
        }
        return corners;
    }

    /// <summary>
    /// Build a HexLayoutViewModel from a list of tiles at the given hex size.
    /// Normalizes the viewbox so minX/minY are 0 after a 20-unit padding shift.
    /// Returns an empty ViewModel (ViewBoxWidth=0, ViewBoxHeight=0) for empty tile lists.
    /// </summary>
    public static HexLayoutViewModel Build(IList<TileDto> tiles, double hexSize)
    {
        var vm = new HexLayoutViewModel();
        if (tiles.Count == 0) return vm;

        const double pad = 20;

        // Compute raw centers and corners for every tile
        var rawCenters = new Dictionary<Guid, (double X, double Y)>();
        var rawCorners = new Dictionary<Guid, List<Point>>();

        foreach (var tile in tiles)
        {
            var (cx, cy) = AxialToPixel(tile.CoordQ, tile.CoordR, hexSize);
            rawCenters[tile.Id] = (cx, cy);
            rawCorners[tile.Id] = HexCorners(cx, cy, hexSize);
        }

        // Find bounding box across all corner points (not just centers)
        var allXs = rawCorners.Values.SelectMany(c => c).Select(p => p.X).ToList();
        var allYs = rawCorners.Values.SelectMany(c => c).Select(p => p.Y).ToList();
        double minX = allXs.Min() - pad;
        double maxX = allXs.Max() + pad;
        double minY = allYs.Min() - pad;
        double maxY = allYs.Max() + pad;

        // Translation offset to normalize so top-left = (0,0)
        double dx = -minX;
        double dy = -minY;

        vm.ViewBoxWidth = maxX - minX;
        vm.ViewBoxHeight = maxY - minY;

        foreach (var (id, c) in rawCenters)
            vm.CentersByTileId[id] = new HexLayoutViewModel.Point(c.X + dx, c.Y + dy);

        foreach (var (id, cs) in rawCorners)
            vm.CornersByTileId[id] = cs.Select(p => new HexLayoutViewModel.Point(p.X + dx, p.Y + dy)).ToList();

        return vm;
    }
}
