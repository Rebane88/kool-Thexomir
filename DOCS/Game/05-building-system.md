# Systems Reference — Building System

> Exact rules for building placement, upgrades, and unlock requirements.

---

## Core Rules

- **One building per tile** — enforced by unique constraint on `Building.TileId`
- A building can only be constructed on a tile **owned by the kingdom building it**
- The tile must have no existing building (or the existing building must be the prerequisite for an upgrade)
- Certain buildings require specific terrain — `BuildingType.RequiredTerrain`

---

## Building Unlock Chain

To construct a Tier 2 or Tier 3 building, the **previous tier must already exist on the same tile**.

```
Example: To build a Windmill on Tile A, Tile A must already have a Farm.
```

This is enforced by checking `BuildingType.UnlockedByBuildingTypeId`:
- Find the building currently on the tile
- Its `BuildingTypeId` must match the `UnlockedByBuildingTypeId` of the building being constructed

**⚠️ OPEN QUESTION:** When upgrading a building, is the Tier 1 building replaced by Tier 2 (one building per tile), or does Tier 2 stack on top? Current assumption: **replaced** (one building per tile enforced). Confirm.

---

## Terrain Restrictions

Some buildings require a specific terrain type (`BuildingType.RequiredTerrain`). If set, construction is blocked on non-matching tiles.

**⚠️ OPEN QUESTION:** Are any of the seeded buildings terrain-restricted? The schema supports it but no restrictions are currently defined. Candidates to consider: Lumber Camp restricted to Forest, Quarry restricted to Mountain. Decision would add geographic strategy depth.

---

## Building Costs

All costs are defined on `BuildingType`. Apply faction modifier:

```
actualCost = BuildingType.CostX × FactionType.BuildingCostModifier
```

**⚠️ OPEN QUESTION:** Exact costs per BuildingType not yet seeded. See resource-system.md for suggested yield values — costs should be set relative to yield payoff time. Suggested principle: a building should "pay for itself" in roughly 5–8 turns at base yield.

---

## Capturing Buildings

When a tile changes ownership via combat:
- All buildings on the tile remain intact
- `Building.KingdomId` does **not** update — **⚠️ OPEN QUESTION:** Should `Building.KingdomId` update to the new owner when a tile is captured? Currently the field exists but ownership is implied via the tile owner. Clarify whether KingdomId on Building needs to stay in sync or is only used at construction time.

---

## Military Buildings

Military buildings (Barracks, Stables, War Academy) do not produce resources. They exist to unlock unit types for recruitment.

| Building | Unlocks |
|----------|---------|
| Barracks | Swordsman, Archer |
| Stables | Knight |
| Wizard Tower | Mage |
| War Academy | Catapult |

A unit can only be recruited if the **required building exists on the tile the army is currently on**.

**⚠️ OPEN QUESTION:** Can you recruit units onto a tile that has the building, and then move the army away — leaving the tile with a building but no army? Yes — confirm this is valid and intended.

---

## Defense Buildings

Defense buildings (Palisade, Stone Wall, Fortress) increase the effective terrain defense bonus of a tile.

**⚠️ OPEN QUESTION:** The current schema has no field for defense buildings adding a bonus — TerrainType.DefenseBonus is fixed. Options:
- A) Add a `DefenseBonus` field to `BuildingType` that stacks with terrain bonus
- B) Define defense buildings as adding a flat modifier (e.g. Fortress = +20% additional defense)
- C) Defense buildings simply improve the visual but use a separate game mechanic (e.g. Fortress = attacker takes casualties even when winning)

**This needs a decision before implementing combat with fortified tiles.** Catapult's "strong against fortified tiles" ability currently has no mechanical hook.

---

## Building Construction Sequence (API)

1. Validate tile is owned by the requesting kingdom
2. Validate tile has no existing building (or existing building is the prerequisite)
3. Validate terrain restriction if applicable
4. Validate kingdom has sufficient resources
5. Deduct resources from KingdomResource
6. Create Building record
7. Create TurnLog entry with EventType = BuildingConstructed
