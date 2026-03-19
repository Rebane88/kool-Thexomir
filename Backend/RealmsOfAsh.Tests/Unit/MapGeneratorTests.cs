using Domain.Map;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

[Trait("Category", "Unit")]
public class MapGeneratorTests
{
    #region GetMapSize

    [Theory]
    [InlineData(2, 16, 16)]
    [InlineData(3, 20, 20)]
    [InlineData(4, 24, 24)]
    public void GetMapSize_ReturnsCorrectDimensions(int playerCount, int expectedWidth, int expectedHeight)
    {
        var (width, height) = MapGenerator.GetMapSize(playerCount);

        width.ShouldBe(expectedWidth);
        height.ShouldBe(expectedHeight);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetMapSize_ThrowsForInvalidPlayerCount(int playerCount)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MapGenerator.GetMapSize(playerCount));
    }

    #endregion

    #region GenerateRectangularHexGrid

    [Theory]
    [InlineData(16, 16, 256)]
    [InlineData(20, 20, 400)]
    [InlineData(24, 24, 576)]
    public void GenerateRectangularHexGrid_ReturnsCorrectTileCount(int width, int height, int expectedCount)
    {
        var coords = MapGenerator.GenerateRectangularHexGrid(width, height);

        coords.Count.ShouldBe(expectedCount);
    }

    [Fact]
    public void GenerateRectangularHexGrid_AllCoordinatesUnique()
    {
        var coords = MapGenerator.GenerateRectangularHexGrid(16, 16);

        var distinct = coords.Distinct().ToList();
        distinct.Count.ShouldBe(coords.Count);
    }

    #endregion

    #region CalculateEdgeStartingPositions

    [Fact]
    public void CalculateEdgeStartingPositions_2Players_OppositeEdges()
    {
        var positions = MapGenerator.CalculateEdgeStartingPositions(16, 16, 2);

        positions.Count.ShouldBe(2);
        // Opposite edges: one near left, one near right
        var qValues = positions.Select(p => p.q).ToList();
        qValues.Min().ShouldBeLessThan(4);
        qValues.Max().ShouldBeGreaterThan(12);
    }

    [Fact]
    public void CalculateEdgeStartingPositions_3Players_TrianglePattern()
    {
        var positions = MapGenerator.CalculateEdgeStartingPositions(20, 20, 3);

        positions.Count.ShouldBe(3);
        // Should form a triangle: 2 near top, 1 near bottom
        positions.ShouldBeUnique();
    }

    [Fact]
    public void CalculateEdgeStartingPositions_4Players_Corners()
    {
        var positions = MapGenerator.CalculateEdgeStartingPositions(24, 24, 4);

        positions.Count.ShouldBe(4);
        positions.ShouldBeUnique();
    }

    [Fact]
    public void CalculateEdgeStartingPositions_AllWithinGrid()
    {
        var grid = MapGenerator.GenerateRectangularHexGrid(16, 16);
        var gridSet = grid.ToHashSet();
        var positions = MapGenerator.CalculateEdgeStartingPositions(16, 16, 2);

        foreach (var pos in positions)
        {
            gridSet.ShouldContain(pos, $"Starting position ({pos.q},{pos.r}) not in grid");
        }
    }

    [Fact]
    public void CalculateEdgeStartingPositions_SufficientDistance()
    {
        // For 2 players on 16x16, positions should be well separated
        var positions = MapGenerator.CalculateEdgeStartingPositions(16, 16, 2);

        var (q1, r1) = positions[0];
        var (q2, r2) = positions[1];
        // Hex distance: max of abs differences in cube coords
        int s1 = -q1 - r1;
        int s2 = -q2 - r2;
        int distance = (Math.Abs(q1 - q2) + Math.Abs(r1 - r2) + Math.Abs(s1 - s2)) / 2;

        distance.ShouldBeGreaterThan(8, "Starting positions should be far apart");
    }

    #endregion
}
