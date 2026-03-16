using Domain.Map;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

public class HexGridTests
{
    [Fact]
    public void GenerateHexGrid_radius_0_returns_1_tile_at_origin()
    {
        var tiles = HexGridHelper.GenerateHexGrid(0);

        tiles.Count.ShouldBe(1);
        tiles.ShouldContain((0, 0));
    }

    [Fact]
    public void GenerateHexGrid_radius_1_returns_7_tiles()
    {
        var tiles = HexGridHelper.GenerateHexGrid(1);

        tiles.Count.ShouldBe(7);
    }

    [Fact]
    public void GenerateHexGrid_radius_4_returns_61_tiles()
    {
        // Formula: 1 + 3*N*(N+1) = 1 + 3*4*5 = 61
        var tiles = HexGridHelper.GenerateHexGrid(4);

        tiles.Count.ShouldBe(61);
    }

    [Fact]
    public void GetNeighbors_origin_returns_6_neighbors()
    {
        var neighbors = HexGridHelper.GetNeighbors(0, 0);

        neighbors.Count.ShouldBe(6);
        neighbors.ShouldContain((1, 0));
        neighbors.ShouldContain((-1, 0));
        neighbors.ShouldContain((0, 1));
        neighbors.ShouldContain((0, -1));
        neighbors.ShouldContain((1, -1));
        neighbors.ShouldContain((-1, 1));
    }

    [Theory]
    [InlineData(2, 4)]
    [InlineData(4, 6)]
    public void CalculateRadius_returns_playerCount_plus_2(int playerCount, int expectedRadius)
    {
        HexGridHelper.CalculateRadius(playerCount).ShouldBe(expectedRadius);
    }

    [Fact]
    public void CalculateStartingPositions_returns_correct_count()
    {
        var positions = HexGridHelper.CalculateStartingPositions(6, 4);

        positions.Count.ShouldBe(4);
    }

    [Fact]
    public void CalculateStartingPositions_all_within_map_radius()
    {
        var mapRadius = 6;
        var positions = HexGridHelper.CalculateStartingPositions(mapRadius, 4);

        foreach (var pos in positions)
        {
            HexGridHelper.IsWithinRadius(pos, mapRadius).ShouldBeTrue(
                $"Position ({pos.q}, {pos.r}) should be within radius {mapRadius}");
        }
    }

    [Fact]
    public void IsWithinRadius_center_is_always_within()
    {
        HexGridHelper.IsWithinRadius((0, 0), 0).ShouldBeTrue();
        HexGridHelper.IsWithinRadius((0, 0), 5).ShouldBeTrue();
    }

    [Fact]
    public void IsWithinRadius_edge_tile_is_within()
    {
        HexGridHelper.IsWithinRadius((3, 0), 3).ShouldBeTrue();
        HexGridHelper.IsWithinRadius((3, 0), 2).ShouldBeFalse();
    }
}
