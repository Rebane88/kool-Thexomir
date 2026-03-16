using Application.Contracts;
using Application.Services.GameInitialization.DTOs;
using DomainBuilding = Domain.Buildings.Building;
using Domain.Game;
using Domain.Map;
using Domain.Resources;

namespace Application.Services.GameInitialization;

public class GameInitializationService(IUnitOfWork unitOfWork) : IGameInitializationService
{
    private static readonly Guid PlainsTerrainId = new("AAAAAAAA-0001-0000-0000-000000000001");
    private static readonly Guid CapitalBuildingTypeId = new("BBBBBBBB-0001-0000-0000-000000000100");

    public async Task<GameStateDto> InitializeGameAsync(Guid gameId)
    {
        var game = await unitOfWork.Games.GetByIdForUpdateAsync(gameId)
                   ?? throw new InvalidOperationException($"Game {gameId} not found.");

        var kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId);
        var terrainTypes = (await unitOfWork.TerrainTypes.GetAllAsync()).ToList();

        // Generate hex grid
        int radius = HexGridHelper.CalculateRadius(kingdoms.Count);
        var hexCoords = HexGridHelper.GenerateHexGrid(radius);

        // Zone-based terrain assignment
        var terrainAssignment = AssignZoneBasedTerrain(hexCoords, terrainTypes, radius);

        // Create tile entities
        var tiles = hexCoords.Select(coord => new Tile
        {
            GameId = gameId,
            CoordQ = coord.q,
            CoordR = coord.r,
            TerrainTypeId = terrainAssignment[coord],
        }).ToList();

        // Build a lookup for quick coordinate -> tile access
        var tileLookup = tiles.ToDictionary(t => (t.CoordQ, t.CoordR));

        // Calculate starting positions and assign kingdoms
        var positions = HexGridHelper.CalculateStartingPositions(radius, kingdoms.Count);
        var allBuildings = new List<DomainBuilding>();
        var allResources = new List<KingdomResource>();

        for (int i = 0; i < kingdoms.Count; i++)
        {
            var kingdom = kingdoms[i];
            var center = positions[i];

            // Assign center tile to kingdom
            if (!tileLookup.TryGetValue(center, out var centerTile))
                throw new InvalidOperationException($"Starting position ({center.q}, {center.r}) is outside the grid.");

            centerTile.KingdomId = kingdom.Id;
            centerTile.TerrainTypeId = PlainsTerrainId; // Fair starting terrain

            // Assign neighbor tiles to kingdom
            var neighbors = HexGridHelper.GetNeighbors(center.q, center.r);
            foreach (var neighbor in neighbors)
            {
                if (HexGridHelper.IsWithinRadius(neighbor, radius) && tileLookup.TryGetValue(neighbor, out var neighborTile))
                {
                    neighborTile.KingdomId = kingdom.Id;
                }
            }

            // Create Capital building on center tile
            var capital = new DomainBuilding
            {
                TileId = centerTile.Id,
                BuildingTypeId = CapitalBuildingTypeId,
            };
            allBuildings.Add(capital);

            // Create KingdomResource rows from faction starting values
            var faction = await unitOfWork.FactionTypes.GetByIdAsync(kingdom.FactionTypeId!.Value)
                          ?? throw new InvalidOperationException($"FactionType {kingdom.FactionTypeId} not found.");

            allResources.AddRange(new[]
            {
                new KingdomResource { KingdomId = kingdom.Id, ResourceType = EResourceType.Gold, Amount = faction.StartingGold },
                new KingdomResource { KingdomId = kingdom.Id, ResourceType = EResourceType.Food, Amount = faction.StartingFood },
                new KingdomResource { KingdomId = kingdom.Id, ResourceType = EResourceType.Wood, Amount = faction.StartingWood },
                new KingdomResource { KingdomId = kingdom.Id, ResourceType = EResourceType.Stone, Amount = faction.StartingStone },
                new KingdomResource { KingdomId = kingdom.Id, ResourceType = EResourceType.Mana, Amount = faction.StartingMana },
            });
        }

        // Bulk add all tiles
        foreach (var tile in tiles)
            await unitOfWork.Tiles.AddAsync(tile);

        // Add all buildings
        foreach (var building in allBuildings)
            await unitOfWork.Buildings.AddAsync(building);

        // Add all resources
        foreach (var resource in allResources)
            await unitOfWork.KingdomResources.AddAsync(resource);

        // Update game metadata (Status already set to InProgress by LobbyService.StartGameAsync)
        game.TurnNumber = 1;
        game.MapWidth = radius;
        await unitOfWork.Games.UpdateAsync(game);

        await unitOfWork.CommitAsync();

        return BuildGameStateDtoFromMemory(game, tiles, kingdoms, allBuildings, allResources);
    }

    public async Task<GameStateDto> BuildGameStateSnapshotAsync(Guid gameId)
    {
        var game = await unitOfWork.Games.GetByIdAsync(gameId)
                   ?? throw new InvalidOperationException($"Game {gameId} not found.");

        var tiles = await unitOfWork.Tiles.GetTilesForGameAsync(gameId);
        var kingdoms = await unitOfWork.Kingdoms.GetKingdomsForGameAsync(gameId);

        var allResources = new List<KingdomResource>();
        foreach (var kingdom in kingdoms)
        {
            var resources = await unitOfWork.KingdomResources.GetResourcesForKingdomAsync(kingdom.Id);
            allResources.AddRange(resources);
        }

        var allBuildings = tiles
            .Where(t => t.Buildings != null)
            .SelectMany(t => t.Buildings!)
            .ToList();

        return BuildGameStateDtoFromMemory(game, tiles, kingdoms, allBuildings, allResources);
    }

    private static Dictionary<(int q, int r), Guid> AssignZoneBasedTerrain(
        List<(int q, int r)> hexCoords,
        List<TerrainType> terrainTypes,
        int radius)
    {
        // Create 5-8 zone centers randomly distributed across the grid
        int zoneCount = Math.Clamp(terrainTypes.Count + Random.Shared.Next(0, 4), 5, 8);
        var zoneCenters = new List<(int q, int r, Guid terrainTypeId)>();

        for (int i = 0; i < zoneCount; i++)
        {
            // Pick a random coordinate within the grid
            (int q, int r) candidate;
            do
            {
                candidate = (Random.Shared.Next(-radius, radius + 1), Random.Shared.Next(-radius, radius + 1));
            } while (!HexGridHelper.IsWithinRadius(candidate, radius));

            var terrain = terrainTypes[Random.Shared.Next(terrainTypes.Count)];
            zoneCenters.Add((candidate.q, candidate.r, terrain.Id));
        }

        // Assign each tile to the nearest zone center
        var assignment = new Dictionary<(int q, int r), Guid>();
        foreach (var coord in hexCoords)
        {
            int minDist = int.MaxValue;
            Guid closestTerrain = terrainTypes[0].Id;

            foreach (var zone in zoneCenters)
            {
                int dq = coord.q - zone.q;
                int dr = coord.r - zone.r;
                int dist = Math.Max(Math.Max(Math.Abs(dq), Math.Abs(dr)), Math.Abs(dq + dr));

                if (dist < minDist)
                {
                    minDist = dist;
                    closestTerrain = zone.terrainTypeId;
                }
            }

            assignment[coord] = closestTerrain;
        }

        return assignment;
    }

    private static GameStateDto BuildGameStateDtoFromMemory(
        Game game,
        List<Tile> tiles,
        List<Kingdom> kingdoms,
        List<DomainBuilding> buildings,
        List<KingdomResource> resources)
    {
        var buildingsByTile = buildings
            .GroupBy(b => b.TileId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var resourcesByKingdom = resources
            .GroupBy(r => r.KingdomId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return new GameStateDto
        {
            GameId = game.Id,
            Status = game.Status.ToString(),
            TurnNumber = game.TurnNumber,
            WinCondition = game.WinCondition.ToString(),
            MapRadius = game.MapWidth,
            Tiles = tiles.Select(t => new TileDto
            {
                Id = t.Id,
                CoordQ = t.CoordQ,
                CoordR = t.CoordR,
                TerrainTypeId = t.TerrainTypeId,
                TerrainName = t.TerrainType?.Name.Translate() ?? string.Empty,
                KingdomId = t.KingdomId,
                Buildings = buildingsByTile.TryGetValue(t.Id, out var tileBuildings)
                    ? tileBuildings.Select(b => new BuildingDto
                    {
                        Id = b.Id,
                        BuildingTypeId = b.BuildingTypeId,
                        BuildingName = b.BuildingType?.Name.Translate() ?? string.Empty,
                    }).ToList()
                    : [],
            }).ToList(),
            Kingdoms = kingdoms.Select(k => new KingdomDto
            {
                Id = k.Id,
                Name = k.Name,
                UserId = k.AppUserId,
                FactionTypeId = k.FactionTypeId,
                FactionName = k.FactionType?.Name.Translate(),
                IsEliminated = k.IsEliminated,
                Resources = resourcesByKingdom.TryGetValue(k.Id, out var kingdomResources)
                    ? kingdomResources.Select(r => new KingdomResourceDto
                    {
                        ResourceType = r.ResourceType.ToString(),
                        Amount = r.Amount,
                    }).ToList()
                    : [],
            }).ToList(),
        };
    }
}
