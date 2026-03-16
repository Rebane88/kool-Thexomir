using Base;
using Domain.Map;
using Domain.Resources;

namespace Domain.Game;

public class Game : BaseEntity
{
    public EGameStatus Status { get; set; }
    public int TurnNumber { get; set; }
    public EWinCondition WinCondition { get; set; }
    public int MaxPlayers { get; set; }
    public string LobbyCode { get; set; } = string.Empty; // 6-char unique code
    public int MapWidth { get; set; }
    public int MapHeight { get; set; }
    public Guid? HostUserId { get; set; }
    public Guid? CurrentTurnKingdomId { get; set; }
    public uint xmin { get; set; } // PostgreSQL xmin system column — concurrency token for lobby join race protection

    // Navigation
    public Kingdom? CurrentTurnKingdom { get; set; }
    public ICollection<Kingdom>? Kingdoms { get; set; }
    public ICollection<Tile>? Tiles { get; set; }
    public ICollection<TurnLog>? TurnLogs { get; set; }

    // -------------------------------------------------------------------------
    // Domain behavior
    // -------------------------------------------------------------------------

    /// <summary>
    /// Advances the turn to the next non-eliminated kingdom in round-robin order.
    /// Increments TurnNumber when play wraps back to the first active kingdom.
    /// Requires Kingdoms navigation to be loaded with non-null collection.
    /// </summary>
    public Kingdom AdvanceTurn()
    {
        var activeKingdoms = Kingdoms!
            .Where(k => !k.IsEliminated)
            .OrderBy(k => k.CreatedAt)
            .ThenBy(k => k.Id)
            .ToList();

        var currentIndex = activeKingdoms.FindIndex(k => k.Id == CurrentTurnKingdomId);
        var nextIndex = (currentIndex + 1) % activeKingdoms.Count;

        if (nextIndex <= currentIndex)
            TurnNumber++;

        var nextKingdom = activeKingdoms[nextIndex];
        CurrentTurnKingdomId = nextKingdom.Id;
        return nextKingdom;
    }

    /// <summary>
    /// Calculates resource income for a kingdom based on its buildings, terrain, and faction bonuses.
    /// Formula per building per resource: floor(baseYield * terrainMultiplier * factionMultiplier)
    /// Terrain multiplier = 1.25 if terrain bonus matches resource type, else 1.0.
    /// </summary>
    public static Dictionary<EResourceType, int> CalculateIncome(
        IEnumerable<Tile> tilesWithBuildings,
        Dictionary<EResourceType, decimal> factionBonuses)
    {
        var income = new Dictionary<EResourceType, int>
        {
            { EResourceType.Gold, 0 },
            { EResourceType.Food, 0 },
            { EResourceType.Wood, 0 },
            { EResourceType.Stone, 0 },
            { EResourceType.Mana, 0 },
        };

        foreach (var tile in tilesWithBuildings)
        {
            if (tile.Buildings is null) continue;
            foreach (var building in tile.Buildings)
            {
                var bt = building.BuildingType!;
                var terrainBonus = tile.TerrainType!.ResourceBonusType;

                AddYield(income, EResourceType.Food, bt.FoodYield, terrainBonus, factionBonuses);
                AddYield(income, EResourceType.Wood, bt.WoodYield, terrainBonus, factionBonuses);
                AddYield(income, EResourceType.Stone, bt.StoneYield, terrainBonus, factionBonuses);
                AddYield(income, EResourceType.Gold, bt.GoldYield, terrainBonus, factionBonuses);
                AddYield(income, EResourceType.Mana, bt.ManaYield, terrainBonus, factionBonuses);
            }
        }

        return income;
    }

    private static void AddYield(
        Dictionary<EResourceType, int> income,
        EResourceType resourceType,
        int baseYield,
        ETerrainResourceBonus terrainBonus,
        Dictionary<EResourceType, decimal> factionBonuses)
    {
        if (baseYield <= 0) return;

        decimal terrainMultiplier = MapTerrainToResource(terrainBonus) == resourceType ? 1.25m : 1.00m;
        decimal factionMultiplier = factionBonuses.TryGetValue(resourceType, out var fm) ? fm : 1.00m;

        int contribution = (int)Math.Floor(baseYield * terrainMultiplier * factionMultiplier);
        income[resourceType] += contribution;
    }

    private static EResourceType? MapTerrainToResource(ETerrainResourceBonus terrain) => terrain switch
    {
        ETerrainResourceBonus.Food => EResourceType.Food,
        ETerrainResourceBonus.Wood => EResourceType.Wood,
        ETerrainResourceBonus.Stone => EResourceType.Stone,
        ETerrainResourceBonus.Gold => EResourceType.Gold,
        ETerrainResourceBonus.Mana => EResourceType.Mana,
        _ => null,
    };
}
