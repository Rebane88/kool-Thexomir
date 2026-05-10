using API.Areas.Public.Helpers;
using API.Areas.Public.ViewModels;
using Application.Services.GameInitialization.DTOs.V1;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit.Areas.Public;

/// <summary>
/// Unit tests for HexLayout.cs — mirrors the JS hex-math.test.ts canonical values.
/// All coordinate tests use size=40 to match the plan spec (hex-math.test.ts uses size=30;
/// the specific numeric assertions below use size=40 per the plan's behavior spec).
/// </summary>
public class HexLayoutTests
{
    // =========================================================================
    // AxialToPixel
    // =========================================================================

    [Fact]
    public void AxialToPixel_Origin_ReturnsZero()
    {
        var (x, y) = HexLayout.AxialToPixel(0, 0, 40);
        x.ShouldBe(0.0, tolerance: 0.001);
        y.ShouldBe(0.0, tolerance: 0.001);
    }

    [Fact]
    public void AxialToPixel_Q1R0_ReturnsHorizontalOffset()
    {
        // q=1, r=0, size=40 → x = 40 * sqrt(3) ≈ 69.282, y = 0
        var (x, y) = HexLayout.AxialToPixel(1, 0, 40);
        x.ShouldBe(40 * Math.Sqrt(3), tolerance: 0.001);
        y.ShouldBe(0.0, tolerance: 0.001);
    }

    [Fact]
    public void AxialToPixel_Q0R1_ReturnsDiagonalOffset()
    {
        // q=0, r=1, size=40 → x = 40 * sqrt(3)/2 ≈ 34.641, y = 60
        var (x, y) = HexLayout.AxialToPixel(0, 1, 40);
        x.ShouldBe(40 * Math.Sqrt(3) / 2.0, tolerance: 0.001);
        y.ShouldBe(60.0, tolerance: 0.001);
    }

    // =========================================================================
    // HexCorners
    // =========================================================================

    [Fact]
    public void HexCorners_AtOrigin_Returns6PointsPointyTop()
    {
        var corners = HexLayout.HexCorners(0, 0, 40);
        corners.Count.ShouldBe(6);

        // First corner is at angle 30° — pointy-top orientation
        // expected: x = 40 * cos(30°) ≈ 34.641, y = 40 * sin(30°) = 20
        var angleRad = Math.PI / 180.0 * 30;
        corners[0].X.ShouldBe(40 * Math.Cos(angleRad), tolerance: 0.001);
        corners[0].Y.ShouldBe(40 * Math.Sin(angleRad), tolerance: 0.001);
    }

    [Fact]
    public void HexCorners_EachCorner_IsExactlySizeFromCenter()
    {
        var corners = HexLayout.HexCorners(100, 100, 40);
        foreach (var corner in corners)
        {
            var dist = Math.Sqrt(Math.Pow(corner.X - 100, 2) + Math.Pow(corner.Y - 100, 2));
            dist.ShouldBe(40.0, tolerance: 0.001);
        }
    }

    // =========================================================================
    // Build
    // =========================================================================

    [Fact]
    public void Build_EmptyList_ReturnsZeroSizedViewBox()
    {
        var vm = HexLayout.Build(new List<TileDto>(), 40);
        vm.ViewBoxWidth.ShouldBe(0.0, tolerance: 0.001);
        vm.ViewBoxHeight.ShouldBe(0.0, tolerance: 0.001);
        vm.CentersByTileId.ShouldBeEmpty();
        vm.CornersByTileId.ShouldBeEmpty();
    }

    [Fact]
    public void Build_SingleTile_NormalizesOriginToZero()
    {
        // Origin tile (0,0) in axial coords → raw pixel center = (0,0)
        // After normalization with padding, center should shift to (padding, padding)
        var tileId = Guid.NewGuid();
        var tile = new TileDto { Id = tileId, CoordQ = 0, CoordR = 0 };

        var vm = HexLayout.Build(new List<TileDto> { tile }, 40);

        vm.CentersByTileId.ShouldContainKey(tileId);
        vm.CornersByTileId.ShouldContainKey(tileId);

        // After normalization, the center of (0,0) is at (padding, padding) = (20, 20)
        vm.CentersByTileId[tileId].X.ShouldBeGreaterThan(0);
        vm.CentersByTileId[tileId].Y.ShouldBeGreaterThan(0);

        // ViewBox should have positive dimensions
        vm.ViewBoxWidth.ShouldBeGreaterThan(0);
        vm.ViewBoxHeight.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Build_TwoTiles_ViewBoxSpansBoth()
    {
        // Two tiles: (0,0) and (2,0) — should produce a wider viewbox than a single tile
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var tiles = new List<TileDto>
        {
            new TileDto { Id = id1, CoordQ = 0, CoordR = 0 },
            new TileDto { Id = id2, CoordQ = 2, CoordR = 0 }
        };

        var singleTileVm = HexLayout.Build(new List<TileDto> { new TileDto { Id = id1, CoordQ = 0, CoordR = 0 } }, 40);
        var twoTileVm = HexLayout.Build(tiles, 40);

        twoTileVm.ViewBoxWidth.ShouldBeGreaterThan(singleTileVm.ViewBoxWidth);
        twoTileVm.CentersByTileId.Count.ShouldBe(2);
        twoTileVm.CornersByTileId.Count.ShouldBe(2);
    }
}
