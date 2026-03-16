using Base;
using Base.Contracts;
using Domain.Buildings;
using Domain.Factions;
using Domain.Map;
using Domain.Military;
using Domain.Resources;

namespace Domain.Game;

public record CombatResult(
    Battle Battle,
    Guid? WinnerKingdomId,
    bool TileCaptured,
    decimal AttackerStrength,
    decimal DefenderStrength,
    List<(Guid UnitTypeId, string UnitTypeName, int Before, int Lost, int After)> AttackerCasualties,
    List<(Guid UnitTypeId, string UnitTypeName, int Before, int Lost, int After)> DefenderCasualties,
    bool AttackerArmyDestroyed,
    bool DefenderArmyDestroyed);

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
    public int? MaxTurnCount { get; set; }
    public Guid? WinnerKingdomId { get; set; }
    public uint xmin { get; set; } // PostgreSQL xmin system column — concurrency token for lobby join race protection

    // Navigation
    public Kingdom? CurrentTurnKingdom { get; set; }
    public Kingdom? WinnerKingdom { get; set; }
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

    /// <summary>
    /// Validates and places a building on a tile. Validates tile ownership, one-building-per-tile,
    /// prerequisite chain, and resource affordability. Deducts costs atomically (all-or-nothing).
    /// Returns the created Building on success, or a failure Result with error message.
    /// </summary>
    public Result<Building> PlaceBuilding(
        Guid tileId,
        BuildingType buildingType,
        Tile tile,
        ICollection<Building> existingKingdomBuildings,
        ICollection<KingdomResource> kingdomResources,
        decimal factionCostModifier)
    {
        // Determine which kingdom is placing (current turn kingdom)
        var kingdom = Kingdoms!.SingleOrDefault(k => k.Id == CurrentTurnKingdomId);
        if (kingdom is null)
            return Result<Building>.Fail("Current turn kingdom not found.");

        // Validate tile ownership
        if (tile.KingdomId != kingdom.Id)
            return Result<Building>.Fail("You do not own this tile.");

        // Check one building per tile
        if (existingKingdomBuildings.Any(b => b.TileId == tileId))
            return Result<Building>.Fail("This tile already has a building.");

        // Check prerequisite chain
        if (buildingType.PrerequisiteBuildingTypeId.HasValue)
        {
            var hasPrerequisite = existingKingdomBuildings.Any(
                b => b.BuildingTypeId == buildingType.PrerequisiteBuildingTypeId.Value);
            if (!hasPrerequisite)
                return Result<Building>.Fail("Missing prerequisite building. Build the required lower-tier building first.");
        }

        // Calculate costs with faction modifier
        var costs = new Dictionary<EResourceType, int>
        {
            { EResourceType.Gold, (int)Math.Floor(buildingType.GoldCost * factionCostModifier) },
            { EResourceType.Wood, (int)Math.Floor(buildingType.WoodCost * factionCostModifier) },
            { EResourceType.Stone, (int)Math.Floor(buildingType.StoneCost * factionCostModifier) },
            { EResourceType.Mana, (int)Math.Floor(buildingType.ManaCost * factionCostModifier) },
        };

        // Validate ALL costs first (all-or-nothing)
        foreach (var (type, cost) in costs.Where(c => c.Value > 0))
        {
            var resource = kingdomResources.SingleOrDefault(r => r.ResourceType == type);
            if (resource is null || resource.Amount < cost)
                return Result<Building>.Fail($"Not enough {type}. Need {cost}, have {(int)(resource?.Amount ?? 0)}.");
        }

        // Deduct ALL costs (mutates the passed-in resources)
        foreach (var (type, cost) in costs.Where(c => c.Value > 0))
        {
            var resource = kingdomResources.Single(r => r.ResourceType == type);
            resource.Amount -= cost;
        }

        // Create building
        var building = new Building
        {
            TileId = tileId,
            BuildingTypeId = buildingType.Id,
        };

        return Result<Building>.Ok(building);
    }

    /// <summary>
    /// Validates and trains troops at a building. Validates building ownership, unit production capability,
    /// one-training-per-turn limit, and resource affordability. Deducts costs atomically.
    /// Returns the Army (new or existing) and the list of Unit entries.
    /// </summary>
    public Result<(Army army, List<Unit> units)> TrainTroops(
        Building building,
        BuildingUnitType? buildingUnitType,
        UnitType unitType,
        int quantity,
        Army? existingArmyOnTile,
        Tile tile,
        ICollection<KingdomResource> kingdomResources,
        decimal factionCostModifier)
    {
        var kingdom = Kingdoms!.SingleOrDefault(k => k.Id == CurrentTurnKingdomId);
        if (kingdom is null)
            return Result<(Army, List<Unit>)>.Fail("Current turn kingdom not found.");

        // Validate tile ownership (building's tile must be owned by current kingdom)
        if (tile.KingdomId != kingdom.Id)
            return Result<(Army, List<Unit>)>.Fail("You do not own the tile this building is on.");

        // Validate building can produce this unit type
        if (buildingUnitType is null)
            return Result<(Army, List<Unit>)>.Fail("This building cannot produce that unit type.");

        // Validate one training per building per turn
        if (building.HasTrainedThisTurn)
            return Result<(Army, List<Unit>)>.Fail("This building has already trained troops this turn.");

        // Validate quantity > 0
        if (quantity <= 0)
            return Result<(Army, List<Unit>)>.Fail("Quantity must be greater than zero.");

        // Calculate costs with faction modifier (per resource type, floored)
        var costs = new Dictionary<EResourceType, int>
        {
            { EResourceType.Gold, (int)Math.Floor(unitType.GoldCost * quantity * factionCostModifier) },
            { EResourceType.Food, (int)Math.Floor(unitType.FoodCost * quantity * factionCostModifier) },
            { EResourceType.Wood, (int)Math.Floor(unitType.WoodCost * quantity * factionCostModifier) },
            { EResourceType.Stone, (int)Math.Floor(unitType.StoneCost * quantity * factionCostModifier) },
            { EResourceType.Mana, (int)Math.Floor(unitType.ManaCost * quantity * factionCostModifier) },
        };

        // All-or-nothing cost validation
        foreach (var (type, cost) in costs.Where(c => c.Value > 0))
        {
            var resource = kingdomResources.SingleOrDefault(r => r.ResourceType == type);
            if (resource is null || resource.Amount < cost)
                return Result<(Army, List<Unit>)>.Fail($"Not enough {type}. Need {cost}, have {(int)(resource?.Amount ?? 0)}.");
        }

        // Deduct costs
        foreach (var (type, cost) in costs.Where(c => c.Value > 0))
        {
            var resource = kingdomResources.Single(r => r.ResourceType == type);
            resource.Amount -= cost;
        }

        // Mark building as trained this turn
        building.HasTrainedThisTurn = true;

        // Create or merge army
        var army = existingArmyOnTile ?? new Army
        {
            TileId = tile.Id,
            KingdomId = kingdom.Id,
            Units = new List<Unit>()
        };

        var units = army.Units!.ToList();

        // Check if army already has units of this type -> merge quantity
        var existingUnit = units.FirstOrDefault(u => u.UnitTypeId == unitType.Id);
        if (existingUnit != null)
        {
            existingUnit.Quantity += quantity;
        }
        else
        {
            var newUnit = new Unit
            {
                ArmyId = army.Id,
                UnitTypeId = unitType.Id,
                Quantity = quantity,
                UnitType = unitType
            };
            units.Add(newUnit);
            army.Units!.Add(newUnit);
        }

        return Result<(Army, List<Unit>)>.Ok((army, units));
    }

    /// <summary>
    /// Validates and moves an army to an adjacent tile. Validates adjacency, blocks enemy-occupied tiles,
    /// claims unowned tiles, and auto-merges with friendly armies on the destination.
    /// </summary>
    public Result<(bool tileClaimed, bool armyMerged, Army? mergedIntoArmy)> MoveArmy(
        Army army,
        Tile sourceTile,
        Tile targetTile,
        Army? existingFriendlyArmy,
        Army? existingEnemyArmy)
    {
        var kingdom = Kingdoms!.SingleOrDefault(k => k.Id == CurrentTurnKingdomId);
        if (kingdom is null)
            return Result<(bool, bool, Army?)>.Fail("Current turn kingdom not found.");

        // Validate army belongs to current kingdom
        if (army.KingdomId != kingdom.Id)
            return Result<(bool, bool, Army?)>.Fail("This army does not belong to your kingdom.");

        // Validate adjacency using HexGridHelper
        var neighbors = HexGridHelper.GetNeighbors(sourceTile.CoordQ, sourceTile.CoordR);
        if (!neighbors.Contains((targetTile.CoordQ, targetTile.CoordR)))
            return Result<(bool, bool, Army?)>.Fail("Target tile is not adjacent.");

        // Block move onto enemy-occupied tile
        if (existingEnemyArmy != null)
            return Result<(bool, bool, Army?)>.Fail("Cannot move into enemy-occupied tile. Use attack instead.");

        // Claim unowned tile
        bool tileClaimed = false;
        if (targetTile.KingdomId is null)
        {
            targetTile.KingdomId = kingdom.Id;
            tileClaimed = true;
        }

        // Auto-merge with friendly army
        bool armyMerged = false;
        Army? mergedIntoArmy = null;
        if (existingFriendlyArmy != null)
        {
            foreach (var unit in army.Units!)
            {
                var matchingUnit = existingFriendlyArmy.Units!.FirstOrDefault(u => u.UnitTypeId == unit.UnitTypeId);
                if (matchingUnit != null)
                {
                    matchingUnit.Quantity += unit.Quantity;
                }
                else
                {
                    existingFriendlyArmy.Units!.Add(new Unit
                    {
                        ArmyId = existingFriendlyArmy.Id,
                        UnitTypeId = unit.UnitTypeId,
                        Quantity = unit.Quantity,
                        UnitType = unit.UnitType
                    });
                }
            }
            armyMerged = true;
            mergedIntoArmy = existingFriendlyArmy;
        }
        else
        {
            army.TileId = targetTile.Id;
        }

        return Result<(bool, bool, Army?)>.Ok((tileClaimed, armyMerged, mergedIntoArmy));
    }

    /// <summary>
    /// Calculates the total strength of an army against an opposing army.
    /// Uses weighted average matchup multipliers (weighted by opposing unit quantity proportions)
    /// and faction unit bonuses (specific > global > 1.0 default).
    /// </summary>
    public static decimal CalculateArmyStrength(
        ICollection<Unit> units,
        ICollection<Unit> opposingUnits,
        ICollection<UnitTypeMatchup> matchups,
        ICollection<FactionUnitBonus> factionBonuses)
    {
        int totalOpposingQty = opposingUnits.Sum(u => u.Quantity);
        if (totalOpposingQty == 0) return 0;

        decimal totalStrength = 0;

        foreach (var unit in units)
        {
            // Faction bonus: specific unit type first, then global (null UnitTypeId), then 1.0
            var specificBonus = factionBonuses.FirstOrDefault(b => b.UnitTypeId == unit.UnitTypeId);
            var globalBonus = factionBonuses.FirstOrDefault(b => b.UnitTypeId == null);
            decimal factionMultiplier = specificBonus?.Multiplier ?? globalBonus?.Multiplier ?? 1.0m;

            // Weighted average matchup against all opposing unit types
            decimal weightedMatchup = 0;
            foreach (var opposing in opposingUnits)
            {
                var matchup = matchups.FirstOrDefault(
                    m => m.AttackerTypeId == unit.UnitTypeId && m.DefenderTypeId == opposing.UnitTypeId);
                decimal multiplier = matchup?.Multiplier ?? 1.0m;
                decimal weight = (decimal)opposing.Quantity / totalOpposingQty;
                weightedMatchup += multiplier * weight;
            }

            totalStrength += unit.Quantity * unit.UnitType!.BaseStrength * weightedMatchup * factionMultiplier;
        }

        return totalStrength;
    }

    /// <summary>
    /// Applies proportional casualties to an army. Each unit type loses floor(qty * ratio) units.
    /// Returns a list of (UnitTypeId, UnitTypeName, Before, Lost, After) tuples.
    /// </summary>
    public static List<(Guid UnitTypeId, string UnitTypeName, int Before, int Lost, int After)> ApplyCasualties(
        ICollection<Unit> units, decimal casualtyRatio)
    {
        var casualties = new List<(Guid, string, int, int, int)>();
        foreach (var unit in units)
        {
            int before = unit.Quantity;
            int lost = (int)Math.Floor(before * casualtyRatio);
            unit.Quantity = before - lost;
            casualties.Add((unit.UnitTypeId, unit.UnitType?.Name.Translate() ?? string.Empty, before, lost, unit.Quantity));
        }
        return casualties;
    }

    /// <summary>
    /// Resolves combat between an attacker army and defender army. Validates adjacency,
    /// army ownership, one-attack-per-turn, and presence of defender. Calculates strength
    /// with matchup matrix, faction bonuses, and terrain defense bonus. Applies proportional
    /// casualties. Handles attacker win (capture + advance), defender win, and mutual destruction.
    /// </summary>
    public Result<CombatResult> ResolveCombat(
        Army attackerArmy,
        Tile attackerTile,
        Tile defenderTile,
        Army defenderArmy,
        ICollection<UnitTypeMatchup> matchups,
        ICollection<FactionUnitBonus> attackerFactionBonuses,
        ICollection<FactionUnitBonus> defenderFactionBonuses,
        TerrainType defenderTerrain)
    {
        var kingdom = Kingdoms!.SingleOrDefault(k => k.Id == CurrentTurnKingdomId);
        if (kingdom is null)
            return Result<CombatResult>.Fail("Current turn kingdom not found.");

        // Validate attacker owns the army
        if (attackerArmy.KingdomId != kingdom.Id)
            return Result<CombatResult>.Fail("This army does not belong to your kingdom.");

        // Validate one attack per turn
        if (attackerArmy.HasAttackedThisTurn)
            return Result<CombatResult>.Fail("This army has already attacked this turn.");

        // Validate adjacency
        var neighbors = HexGridHelper.GetNeighbors(attackerTile.CoordQ, attackerTile.CoordR);
        if (!neighbors.Contains((defenderTile.CoordQ, defenderTile.CoordR)))
            return Result<CombatResult>.Fail("Target tile is not adjacent.");

        // Validate defender exists on tile
        if (defenderArmy.Units is null || !defenderArmy.Units.Any(u => u.Quantity > 0))
            return Result<CombatResult>.Fail("No enemy units on target tile.");

        // Calculate strengths
        decimal attackerStrength = CalculateArmyStrength(
            attackerArmy.Units!, defenderArmy.Units!, matchups, attackerFactionBonuses);
        decimal defenderStrength = CalculateArmyStrength(
            defenderArmy.Units!, attackerArmy.Units!, matchups, defenderFactionBonuses);

        // Apply terrain defense bonus to defender
        defenderStrength *= (1 + defenderTerrain.DefenseBonus);

        // Calculate casualty ratios (simultaneous damage)
        decimal attackerCasualtyRatio = attackerStrength > 0
            ? Math.Min(1.0m, defenderStrength / attackerStrength)
            : 1.0m;
        decimal defenderCasualtyRatio = defenderStrength > 0
            ? Math.Min(1.0m, attackerStrength / defenderStrength)
            : 1.0m;

        // Apply casualties
        var attackerCasualties = ApplyCasualties(attackerArmy.Units!, attackerCasualtyRatio);
        var defenderCasualties = ApplyCasualties(defenderArmy.Units!, defenderCasualtyRatio);

        // Mark army as attacked
        attackerArmy.HasAttackedThisTurn = true;

        // Remove zero-quantity units
        var deadAttackerUnits = attackerArmy.Units!.Where(u => u.Quantity <= 0).ToList();
        foreach (var dead in deadAttackerUnits) attackerArmy.Units!.Remove(dead);

        var deadDefenderUnits = defenderArmy.Units!.Where(u => u.Quantity <= 0).ToList();
        foreach (var dead in deadDefenderUnits) defenderArmy.Units!.Remove(dead);

        bool attackerDestroyed = !attackerArmy.Units!.Any();
        bool defenderDestroyed = !defenderArmy.Units!.Any();

        // Determine winner and tile capture
        Guid? winnerKingdomId = null;
        bool tileCaptured = false;

        if (attackerDestroyed && defenderDestroyed)
        {
            // Mutual destruction = draw
            winnerKingdomId = null;
        }
        else if (defenderDestroyed)
        {
            // Attacker wins — capture tile and auto-advance
            winnerKingdomId = kingdom.Id;
            defenderTile.KingdomId = kingdom.Id;
            tileCaptured = true;
            attackerArmy.TileId = defenderTile.Id;
        }
        else
        {
            // Defender wins (attacker destroyed or weakened)
            winnerKingdomId = defenderArmy.KingdomId;
        }

        // Create battle record
        var battle = new Battle
        {
            AttackerArmyId = attackerArmy.Id,
            DefenderArmyId = defenderArmy.Id,
            WinnerId = winnerKingdomId,
            TileId = defenderTile.Id,
            GameId = Id,
            TurnNumber = TurnNumber
        };

        return Result<CombatResult>.Ok(new CombatResult(
            battle, winnerKingdomId, tileCaptured,
            attackerStrength, defenderStrength,
            attackerCasualties, defenderCasualties,
            attackerDestroyed, defenderDestroyed));
    }

    /// <summary>
    /// Checks if the game has ended via the provided win condition checker.
    /// If a winner is determined, transitions Status to Completed and records WinnerKingdomId.
    /// Returns the check result (null = not applicable, WinCheckResult with GameOver=true = game ended).
    /// </summary>
    public WinCheckResult? CheckWinCondition(
        IWinConditionChecker checker,
        IReadOnlyList<Kingdom> kingdoms,
        IReadOnlyList<Tile> tiles)
    {
        var result = checker.Check(this, kingdoms, tiles);
        if (result?.GameOver == true)
        {
            Status = EGameStatus.Completed;
            WinnerKingdomId = result.WinnerKingdomId;
        }
        return result;
    }
}
