# Systems Reference — Resource System

> Exact rules for how resources are generated, spent, and managed. Reference this when implementing income phase logic and spending validation.

---

## The Five Resources

| Resource | Enum Value | Notes |
|----------|------------|-------|
| Gold | Gold | Primary currency — used for almost everything |
| Food | Food | Army upkeep — armies disband without it |
| Wood | Wood | Basic construction |
| Stone | Stone | Advanced construction |
| Mana | Mana | Magical armies only |

Each kingdom has exactly **5 KingdomResource rows**, one per type. Amount cannot go below 0.

---

## Starting Resources

Each kingdom starts with base resources plus their faction's starting bonus.

Base starting resources (admin-editable):

| Resource | Base Amount |
|----------|------------|
| Gold | 150 |
| Food | 60 |
| Wood | 50 |
| Stone | 20 |
| Mana | 0 |

Faction bonuses are added on top of base:

| Faction | Bonus |
|---------|-------|
| Iron Throne | +50 Gold |
| Mage Council | +30 Mana |
| Merchant Republic | +100 Gold |
| Forest Elves | +50 Wood |

---

## Resource Generation

Resources are generated during the **Income Phase** (see turn-structure.md). Income runs for ALL kingdoms simultaneously after the combat phase.

### Formula

For each resource type (Gold, Food, Wood, Stone, Mana), if the building has a non-zero BaseYield for that resource:

```
yield = BuildingType.BaseYield{Resource}
      × terrainMultiplier
      × factionMultiplier
```

**terrainMultiplier:**
- Check if `TerrainType.BonusResourceType` matches the resource being produced
- If yes: multiply by `TerrainType.ResourceMultiplier` (default: **1.10** — all terrains use the same 10% bonus, admin-editable per terrain)
- If no match: multiplier = 1.0

**factionMultiplier:**
- `FactionType.ResourceProductionModifier` — applies globally to all building yields (e.g. Iron Throne = 0.85)

Terrain and faction multipliers are applied **multiplicatively** (stacked). Example: A building on matching terrain with Iron Throne = BaseYield × 1.10 × 0.85.

Tiles without buildings generate no resources.

---

## Spending Resources

Resources are deducted immediately when an action is taken. Server must validate the kingdom has sufficient resources before processing the action — return a 400 error if not.

**Actions and their costs:**

| Action | Resource Cost Source |
|--------|---------------------|
| Build building | `BuildingType.CostGold/Food/Wood/Stone/Mana × FactionType.BuildingCostModifier` |
| Upgrade building | Same as build (higher tier costs defined on BuildingType) |
| Train army | `ArmyType.TrainingCostGold/Food/Stone/Mana × FactionType.TrainingCostModifier` |

Building costs with faction modifier are rounded using **ceil** (round up) to prevent exploiting fractional discounts.

There is no Gold cost to claim tiles — territory expansion happens automatically when building.

---

## Upkeep

Upkeep is collected during Income Phase Step 2 (after resource generation).

For each army in the kingdom's global roster:
```
deduct ArmyType.UpkeepGold from Gold
deduct ArmyType.UpkeepFood from Food
```

All upkeep values are admin-editable and can be set to 0.

### Upkeep Failure (Disbanding)

If after collecting upkeep a resource would go below 0:

1. Floor the resource at 0
2. Identify armies the kingdom can no longer afford
3. Sort by most expensive upkeep first (combined Gold + Food value)
4. Disband armies one by one until remaining upkeep fits within available resources
5. Log each disbanded army to TurnLog

**This means a kingdom that runs out of Food/Gold loses armies automatically.** This is intentional — it's the economic pressure mechanic.

---

## Resource Constraints

- Amount can never go below **0** — enforced at DB level and application level
- No upper storage cap (may be added later if needed for balance)
- `KingdomResource.UpdatedAt` is updated every time Amount changes

---

## Seeded Building Yields

BaseYield values (all admin-editable):

| Building | Tier | Gold | Food | Wood | Stone | Mana |
|----------|------|------|------|------|-------|------|
| Farm | 1 | — | 10 | — | — | — |
| Windmill | 2 | — | 20 | — | — | — |
| Granary | 3 | — | 35 | — | — | — |
| Lumber Camp | 1 | — | — | 8 | — | — |
| Sawmill | 2 | — | — | 18 | — | — |
| Timber Hall | 3 | — | — | 30 | — | — |
| Quarry | 1 | — | — | — | 6 | — |
| Mason | 2 | — | — | — | 14 | — |
| Stoneworks | 3 | — | — | — | 25 | — |
| Market | 1 | 12 | — | — | — | — |
| Trading Post | 2 | 25 | — | — | — | — |
| Bank | 3 | 45 | — | — | — | — |
| Shrine | 1 | — | — | — | — | 5 |
| Wizard Tower | 2 | — | — | — | — | 12 |
| Arcane Sanctum | 3 | — | — | — | — | 22 |
| Castle | — | — | 10 | 10 | 10 | — |

### Slot Machine Spin Cost

Each slot machine spin costs **30 Gold** (see D-30).
