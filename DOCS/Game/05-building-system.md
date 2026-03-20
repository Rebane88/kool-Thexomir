# Systems Reference — Building System

> Exact rules for building placement, upgrades, and territory expansion.

---

## Core Rules

- **One building per tile** — enforced by unique constraint on `Building.TileId`
- A building can only be constructed on a tile **owned by the kingdom building it**
- The tile must have no existing building (for tier 1) or the existing building must be the prerequisite (for upgrades)
- Only **tier 1** buildings can be placed on empty tiles — higher tiers are upgrades
- Terrain gives bonus yield to buildings but does NOT restrict placement

---

## Territory Expansion via Building

When a building is placed on a tile, all **adjacent unowned tiles** automatically become part of the builder's kingdom.

- Only unowned tiles are claimed — building never steals tiles from other players
- Upgrading a building does NOT trigger additional tile claiming (only initial placement)
- This is the **only way to expand territory** — armies do not claim tiles

---

## Building Unlock Chain

To construct a Tier 2 or Tier 3 building, the **previous tier must already exist on the same tile**. The lower tier building is **replaced** by the upgrade (one building per tile).

```
Example: To build a Windmill on Tile A, Tile A must already have a Farm.
Building the Windmill replaces the Farm.
```

This is enforced by checking `BuildingType.UnlockedByBuildingTypeId`:
- Find the building currently on the tile
- Its `BuildingTypeId` must match the `UnlockedByBuildingTypeId` of the building being constructed

---

## Building Chains

### Resource Buildings

| Chain | Tier 1 | Tier 2 | Tier 3 | Resource |
|-------|--------|--------|--------|----------|
| Food | Farm | Windmill | Granary | Food |
| Wood | Lumber Camp | Sawmill | Timber Hall | Wood |
| Stone | Quarry | Mason | Stoneworks | Stone |
| Gold | Market | Trading Post | Bank | Gold |
| Mana | Shrine | Wizard Tower | Arcane Sanctum | Mana |

### Military Buildings

| Chain | Tier 1 | Tier 2 | Tier 3 |
|-------|--------|--------|--------|
| Military | Barracks | Stables | War Academy |

Military buildings do not produce resources. They unlock army types for training.

| Building | Unlocks |
|----------|---------|
| Barracks | Warrior, Scout |
| Stables | Knight, Berserker (+ lower tier unlocks) |
| War Academy | Mage, Guardian (+ lower tier unlocks) |

### Military Building Capacity

Each military building can support up to **3 armies** at a time. Training is instant but the army occupies a slot on the building it was trained from. To train more armies, you need additional military buildings or free up slots by losing armies in combat.

- Upgrading a military building (e.g. Barracks → Stables) **preserves existing army slots** — armies trained from the original building remain tied to it
- Each army is permanently tied to the building it was trained from
- If a military building is destroyed (tile captured), all armies tied to it are **destroyed** — the building is their lifeline

---

## Building Costs

All costs are defined on `BuildingType` (admin-editable). Apply faction modifier:

```
actualCost = BuildingType.CostX × FactionType.BuildingCostModifier
```

Higher tier buildings cost more but produce more. A building should pay for itself in roughly 5-8 rounds at base yield.

### Tier 1 (placed on empty tiles)

| Building | Gold | Wood | Stone | Resource Produced |
|----------|------|------|-------|-------------------|
| Farm | — | 30 | — | Food |
| Lumber Camp | 30 | — | — | Wood |
| Quarry | 40 | 10 | — | Stone |
| Market | — | 40 | — | Gold |
| Shrine | 50 | 20 | — | Mana |
| Barracks | 50 | 30 | — | (military) |

### Tier 2 (upgrades from Tier 1)

| Building | Gold | Wood | Stone | Resource Produced |
|----------|------|------|-------|-------------------|
| Windmill | 60 | 30 | — | Food |
| Sawmill | 50 | — | 20 | Wood |
| Mason | 60 | 30 | — | Stone |
| Trading Post | — | 80 | 30 | Gold |
| Wizard Tower | 80 | — | 40 | Mana |
| Stables | 100 | 50 | 20 | (military) |

### Tier 3 (upgrades from Tier 2)

| Building | Gold | Wood | Stone | Resource Produced |
|----------|------|------|-------|-------------------|
| Granary | 100 | — | 50 | Food |
| Timber Hall | 80 | — | 50 | Wood |
| Stoneworks | 100 | 60 | — | Stone |
| Bank | — | 120 | 60 | Gold |
| Arcane Sanctum | 150 | — | 80 | Mana |
| War Academy | 200 | 80 | 50 | (military) |

---

## Terrain Yield Bonus

Terrain does not restrict building placement, but gives a yield bonus when the terrain's bonus resource matches the building's output:

```
yield = BuildingType.BaseYield{Resource} × TerrainType.ResourceMultiplier (if terrain bonus matches resource)
```

Example: A Farm on Plains produces more Food. A Quarry on Mountain produces more Stone. But you CAN build a Farm on a Mountain — it just won't get the terrain bonus.

---

## Captured Tiles

When a tile is captured via combat:
- The building on the tile is **destroyed**
- The tile ownership transfers to the winner
- The winner must build a new building if they want to use the tile

---

## Castle

Each player starts with a **Castle** on their starting tile. The castle is the heart of the kingdom.

- The castle produces **+10 Food, +10 Wood, +10 Stone** per round (admin-editable) — this is every kingdom's baseline income
- Losing your castle = **elimination from the game**
- The castle is a special building type — it cannot be rebuilt
- Castle battles use **5v5 army pools** instead of the standard 3v3
- The starting tile + 6 adjacent tiles form the player's initial territory

---

## Building Construction Sequence (API)

1. Validate tile is owned by the requesting kingdom
2. Validate tile has no existing building (tier 1) or existing building is the prerequisite (upgrade)
3. Validate kingdom has sufficient resources (after faction modifier)
4. Deduct resources from KingdomResource
5. Create Building record (replacing old building if upgrade)
6. If tier 1 placement: claim all adjacent unowned tiles
7. Create TurnLog entry with EventType = BuildingConstructed or BuildingUpgraded
