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

    /// <summary>
    /// Assign terrain types to all grid coordinates with anti-clustering constraint.
    /// No tile will have more than maxSameNeighbors neighbors of the same terrain type.
    /// Starting positions are always assigned plainsTerrainId.
    /// </summary>
    public static Dictionary<(int q, int r), Guid> AssignTerrainWithAntiClustering(
        List<(int q, int r)> coords,
        List<(int q, int r)> startingPositions,
        Guid[] terrainTypeIds,
        int[] terrainWeights,
        Random random,
        Guid plainsTerrainId,
        int maxSameNeighbors = 2)
    {
        var terrainMap = new Dictionary<(int q, int r), Guid>(coords.Count);
        var startSet = startingPositions.ToHashSet();
        var gridSet = coords.ToHashSet();

        // Assign starting positions first
        foreach (var pos in startingPositions)
        {
            terrainMap[pos] = plainsTerrainId;
        }

        // Assign remaining tiles
        foreach (var coord in coords)
        {
            if (startSet.Contains(coord)) continue;

            var allNeighborCoords = HexGridHelper.GetNeighbors(coord.q, coord.r)
                .Where(n => gridSet.Contains(n))
                .ToList();
            var assignedNeighbors = allNeighborCoords
                .Where(n => terrainMap.ContainsKey(n))
                .ToList();

            // Try up to 20 random selections
            Guid selected = default;
            bool found = false;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                var candidate = WeightedRandom(terrainTypeIds, terrainWeights, random);
                int sameCount = assignedNeighbors.Count(n => terrainMap[n] == candidate);
                if (sameCount <= maxSameNeighbors)
                {
                    selected = candidate;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // Fallback: pick terrain type with fewest same-type neighbors
                int minCount = int.MaxValue;
                foreach (var terrainId in terrainTypeIds)
                {
                    int count = assignedNeighbors.Count(n => terrainMap[n] == terrainId);
                    if (count < minCount)
                    {
                        minCount = count;
                        selected = terrainId;
                    }
                }
            }

            terrainMap[coord] = selected;

            // Post-check: verify anti-clustering holds for this tile AND its already-assigned neighbors.
            // If a neighbor now exceeds the limit due to this assignment, swap this tile's terrain.
            var currentTerrain = terrainMap[coord];
            foreach (var neighbor in assignedNeighbors)
            {
                var neighborTerrain = terrainMap[neighbor];
                var neighborNeighbors = HexGridHelper.GetNeighbors(neighbor.q, neighbor.r)
                    .Where(n => gridSet.Contains(n) && terrainMap.ContainsKey(n))
                    .ToList();
                int neighborSameCount = neighborNeighbors.Count(n => terrainMap[n] == neighborTerrain);
                if (neighborSameCount > maxSameNeighbors)
                {
                    // This tile's assignment caused a neighbor to exceed the limit.
                    // Pick a different terrain for this tile that won't cause violations.
                    foreach (var alt in terrainTypeIds)
                    {
                        if (alt == currentTerrain) continue;
                        terrainMap[coord] = alt;
                        bool valid = true;
                        // Re-check all assigned neighbors
                        foreach (var n2 in assignedNeighbors)
                        {
                            var n2Neighbors = HexGridHelper.GetNeighbors(n2.q, n2.r)
                                .Where(nn => gridSet.Contains(nn) && terrainMap.ContainsKey(nn));
                            int n2Same = n2Neighbors.Count(nn => terrainMap[nn] == terrainMap[n2]);
                            if (n2Same > maxSameNeighbors) { valid = false; break; }
                        }
                        // Also check this tile itself
                        if (valid)
                        {
                            int selfSame = assignedNeighbors.Count(n => terrainMap[n] == alt);
                            if (selfSame > maxSameNeighbors) valid = false;
                        }
                        if (valid) { currentTerrain = alt; break; }
                    }
                    break;
                }
            }
        }

        return terrainMap;
    }

    /// <summary>
    /// Validate that a starting position has sufficient terrain variety within a given radius.
    /// Returns true if at least requiredVariety distinct terrain types exist within radius hexes.
    /// </summary>
    public static bool ValidateStartingAreaVariety(
        (int q, int r) startPos,
        Dictionary<(int q, int r), Guid> terrainMap,
        int radius = 3,
        int requiredVariety = 4)
    {
        var visited = new HashSet<(int q, int r)>();
        var queue = new Queue<((int q, int r) pos, int dist)>();
        var terrainTypes = new HashSet<Guid>();

        queue.Enqueue((startPos, 0));
        visited.Add(startPos);

        while (queue.Count > 0)
        {
            var (pos, dist) = queue.Dequeue();

            if (terrainMap.TryGetValue(pos, out var terrain))
            {
                terrainTypes.Add(terrain);
            }

            if (dist < radius)
            {
                foreach (var neighbor in HexGridHelper.GetNeighbors(pos.q, pos.r))
                {
                    if (visited.Add(neighbor) && terrainMap.ContainsKey(neighbor))
                    {
                        queue.Enqueue((neighbor, dist + 1));
                    }
                }
            }
        }

        return terrainTypes.Count >= requiredVariety;
    }

    /// <summary>
    /// Ensure all starting positions have sufficient terrain variety.
    /// Swaps tiles near deficient starting positions to missing terrain types.
    /// </summary>
    public static void EnsureStartingAreaVariety(
        Dictionary<(int q, int r), Guid> terrainMap,
        List<(int q, int r)> startingPositions,
        Guid[] terrainTypeIds,
        Random random,
        int radius = 3,
        int requiredVariety = 4)
    {
        var startSet = startingPositions.ToHashSet();

        foreach (var startPos in startingPositions)
        {
            for (int attempt = 0; attempt < 50; attempt++)
            {
                if (ValidateStartingAreaVariety(startPos, terrainMap, radius, requiredVariety))
                    break;

                // Find terrain types present near this starting position
                var nearbyTiles = GetTilesWithinRadius(startPos, terrainMap, radius);
                var presentTypes = nearbyTiles.Select(t => terrainMap[t]).Distinct().ToHashSet();
                var missingTypes = terrainTypeIds.Where(id => !presentTypes.Contains(id)).ToList();

                if (missingTypes.Count == 0) break;

                // Find swappable tiles (within radius, not a starting position)
                var swappable = nearbyTiles
                    .Where(t => !startSet.Contains(t))
                    .OrderBy(_ => random.Next())
                    .ToList();

                if (swappable.Count == 0) break;

                // Swap one tile to a missing terrain type
                var tileToSwap = swappable[0];
                var missingType = missingTypes[random.Next(missingTypes.Count)];
                terrainMap[tileToSwap] = missingType;
            }
        }
    }

    /// <summary>Hex distance between two axial coordinates.</summary>
    private static int GetHexDistance((int q, int r) a, (int q, int r) b)
    {
        int dq = Math.Abs(a.q - b.q);
        int dr = Math.Abs(a.r - b.r);
        int ds = Math.Abs((a.q + a.r) - (b.q + b.r));
        return Math.Max(dq, Math.Max(dr, ds));
    }

    /// <summary>Get all tiles within a hex radius of a position using BFS.</summary>
    private static List<(int q, int r)> GetTilesWithinRadius(
        (int q, int r) center,
        Dictionary<(int q, int r), Guid> terrainMap,
        int radius)
    {
        var visited = new HashSet<(int q, int r)>();
        var queue = new Queue<((int q, int r) pos, int dist)>();
        var result = new List<(int q, int r)>();

        queue.Enqueue((center, 0));
        visited.Add(center);

        while (queue.Count > 0)
        {
            var (pos, dist) = queue.Dequeue();

            if (terrainMap.ContainsKey(pos))
            {
                result.Add(pos);
            }

            if (dist < radius)
            {
                foreach (var neighbor in HexGridHelper.GetNeighbors(pos.q, pos.r))
                {
                    if (visited.Add(neighbor) && terrainMap.ContainsKey(neighbor))
                    {
                        queue.Enqueue((neighbor, dist + 1));
                    }
                }
            }
        }

        return result;
    }

    /// <summary>Weighted random selection from terrain type IDs using cumulative distribution.</summary>
    private static Guid WeightedRandom(Guid[] terrainIds, int[] weights, Random random)
    {
        int totalWeight = 0;
        for (int i = 0; i < weights.Length; i++)
            totalWeight += weights[i];

        int roll = random.Next(totalWeight);
        int cumulative = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative)
                return terrainIds[i];
        }

        return terrainIds[^1];
    }

    /// <summary>Convert even-q offset coordinates to axial coordinates.</summary>
    private static (int q, int r) OffsetToAxial(int col, int row)
    {
        int q = col;
        int r = row - (col + (col & 1)) / 2;
        return (q, r);
    }
}
