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

    #region Terrain Helpers

    private static readonly Guid PlainsId = new("AAAAAAAA-0001-0000-0000-000000000001");
    private static readonly Guid ForestId = new("AAAAAAAA-0001-0000-0000-000000000002");
    private static readonly Guid MountainId = new("AAAAAAAA-0001-0000-0000-000000000003");
    private static readonly Guid DesertId = new("AAAAAAAA-0001-0000-0000-000000000004");
    private static readonly Guid MagicGroveId = new("AAAAAAAA-0001-0000-0000-000000000005");

    private static readonly Guid[] TerrainIds = [PlainsId, ForestId, MountainId, DesertId, MagicGroveId];
    private static readonly int[] TerrainWeights = [30, 25, 20, 15, 10];

    #endregion

    #region AssignTerrainWithAntiClustering

    [Fact]
    public void AssignTerrain_AntiClustering_MaxTwoSameNeighbors()
    {
        var grid = MapGenerator.GenerateRectangularHexGrid(16, 16);
        var startPos = MapGenerator.CalculateEdgeStartingPositions(16, 16, 2);
        var random = new Random(42);

        var terrainMap = MapGenerator.AssignTerrainWithAntiClustering(
            grid, startPos, TerrainIds, TerrainWeights, random, PlainsId);

        var gridSet = grid.ToHashSet();
        foreach (var coord in grid)
        {
            var terrain = terrainMap[coord];
            var neighbors = HexGridHelper.GetNeighbors(coord.q, coord.r)
                .Where(n => gridSet.Contains(n))
                .ToList();
            int sameCount = neighbors.Count(n => terrainMap[n] == terrain);
            sameCount.ShouldBeLessThanOrEqualTo(2,
                $"Tile ({coord.q},{coord.r}) has {sameCount} same-type neighbors");
        }
    }

    [Fact]
    public void AssignTerrain_AllTilesAssigned()
    {
        var grid = MapGenerator.GenerateRectangularHexGrid(16, 16);
        var startPos = MapGenerator.CalculateEdgeStartingPositions(16, 16, 2);
        var random = new Random(42);

        var terrainMap = MapGenerator.AssignTerrainWithAntiClustering(
            grid, startPos, TerrainIds, TerrainWeights, random, PlainsId);

        terrainMap.Count.ShouldBe(grid.Count);
        foreach (var coord in grid)
        {
            terrainMap.ShouldContainKey(coord);
        }
    }

    [Fact]
    public void AssignTerrain_StartingPositionsArePlains()
    {
        var grid = MapGenerator.GenerateRectangularHexGrid(16, 16);
        var startPos = MapGenerator.CalculateEdgeStartingPositions(16, 16, 2);
        var random = new Random(42);

        var terrainMap = MapGenerator.AssignTerrainWithAntiClustering(
            grid, startPos, TerrainIds, TerrainWeights, random, PlainsId);

        foreach (var pos in startPos)
        {
            terrainMap[pos].ShouldBe(PlainsId,
                $"Starting position ({pos.q},{pos.r}) should be Plains");
        }
    }

    #endregion

    #region ValidateStartingAreaVariety

    [Fact]
    public void ValidateStartingAreaVariety_SufficientVariety_ReturnsTrue()
    {
        // Manually construct terrain map with 4+ types near origin
        var terrainMap = new Dictionary<(int q, int r), Guid>
        {
            [(0, 0)] = PlainsId,
            [(1, 0)] = ForestId,
            [(-1, 0)] = MountainId,
            [(0, 1)] = DesertId,
            [(0, -1)] = MagicGroveId,
            [(1, -1)] = PlainsId,
            [(-1, 1)] = ForestId,
        };

        var result = MapGenerator.ValidateStartingAreaVariety((0, 0), terrainMap);

        result.ShouldBeTrue();
    }

    [Fact]
    public void ValidateStartingAreaVariety_InsufficientVariety_ReturnsFalse()
    {
        // Only 2 terrain types near origin
        var terrainMap = new Dictionary<(int q, int r), Guid>
        {
            [(0, 0)] = PlainsId,
            [(1, 0)] = PlainsId,
            [(-1, 0)] = ForestId,
            [(0, 1)] = PlainsId,
            [(0, -1)] = ForestId,
            [(1, -1)] = PlainsId,
            [(-1, 1)] = ForestId,
        };

        var result = MapGenerator.ValidateStartingAreaVariety((0, 0), terrainMap);

        result.ShouldBeFalse();
    }

    #endregion

    #region EnsureStartingAreaVariety

    [Fact]
    public void EnsureStartingAreaVariety_FixesInsufficientVariety()
    {
        var grid = MapGenerator.GenerateRectangularHexGrid(16, 16);
        var startPos = MapGenerator.CalculateEdgeStartingPositions(16, 16, 2);
        var random = new Random(42);

        var terrainMap = MapGenerator.AssignTerrainWithAntiClustering(
            grid, startPos, TerrainIds, TerrainWeights, random, PlainsId);

        MapGenerator.EnsureStartingAreaVariety(terrainMap, startPos, TerrainIds, new Random(123));

        foreach (var pos in startPos)
        {
            MapGenerator.ValidateStartingAreaVariety(pos, terrainMap).ShouldBeTrue(
                $"Starting position ({pos.q},{pos.r}) should have 4+ terrain types within 3 hexes");
        }
    }

    #endregion

    #region GenerateMap Integration

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void GenerateMap_IntegrationTest_AllConstraintsSatisfied(int playerCount)
    {
        var random = new Random(42);
        var (width, height) = MapGenerator.GetMapSize(playerCount);
        var grid = MapGenerator.GenerateRectangularHexGrid(width, height);
        var startPos = MapGenerator.CalculateEdgeStartingPositions(width, height, playerCount);
        var terrainMap = MapGenerator.AssignTerrainWithAntiClustering(
            grid, startPos, TerrainIds, TerrainWeights, random, PlainsId);
        MapGenerator.EnsureStartingAreaVariety(terrainMap, startPos, TerrainIds, new Random(123));

        // All tiles assigned
        terrainMap.Count.ShouldBe(grid.Count);

        // Anti-clustering: max 2 same-type neighbors
        var gridSet = grid.ToHashSet();
        foreach (var coord in grid)
        {
            var terrain = terrainMap[coord];
            var neighbors = HexGridHelper.GetNeighbors(coord.q, coord.r)
                .Where(n => gridSet.Contains(n))
                .ToList();
            int sameCount = neighbors.Count(n => terrainMap[n] == terrain);
            sameCount.ShouldBeLessThanOrEqualTo(2);
        }

        // Starting positions are plains
        foreach (var pos in startPos)
        {
            terrainMap[pos].ShouldBe(PlainsId);
        }

        // Starting areas have 4+ terrain variety
        foreach (var pos in startPos)
        {
            MapGenerator.ValidateStartingAreaVariety(pos, terrainMap).ShouldBeTrue();
        }
    }

    #endregion
}
