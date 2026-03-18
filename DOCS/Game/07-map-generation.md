# Systems Reference — Map Generation

> Rules for how the map is generated at game start.

---

## Map Size

Map size is set at lobby creation via `Game.MapWidth` and `Game.MapHeight`.

**⚠️ OPEN QUESTION:** Are there recommended map sizes per player count? Suggested defaults:

| Players | Suggested Size | Total Tiles |
|---------|---------------|-------------|
| 2 | 12 × 12 | 144 |
| 3–4 | 16 × 16 | 256 |
| 5–6 | 20 × 20 | 400 |
| 7–8 | 24 × 24 | 576 |

These are starting suggestions — tune after playtesting.

---

## Coordinate System

Tiles use **hex axial coordinates** (Q, R). Q + R combination must be unique per game.

**⚠️ OPEN QUESTION:** Adjacency calculation for hex grids — define the exact offset formula used for axial coordinates. Standard axial neighbors are: (Q±1, R), (Q, R±1), (Q+1, R-1), (Q-1, R+1). Confirm which system is being used so all tile adjacency checks in combat, claiming, and army movement use the same logic.

---

## Terrain Distribution

Terrain is randomly assigned per tile at generation time using weighted probability.

**⚠️ OPEN QUESTION:** Terrain distribution weights are not defined. Suggested starting weights:

| Terrain | Suggested Weight |
|---------|-----------------|
| Plains | 35% |
| Forest | 25% |
| Mountain | 20% |
| River | 12% |
| Magic Grove | 8% |

These create a mostly open map with interesting terrain pockets. Adjust for desired gameplay feel.

**⚠️ OPEN QUESTION:** Is terrain purely random per tile, or does generation use noise/clustering to create terrain regions (e.g. mountain ranges, river valleys)? Clustered terrain feels more realistic and strategically meaningful but is more complex to implement. Decide based on development priority.

---

## Player Starting Positions

Each player's starting tile has `HasSettlement = true`.

**Placement rules:**
- Players spawn at distributed positions (corners/edges) to ensure fair starting distance
- **⚠️ OPEN QUESTION:** Exact spawn placement algorithm not defined. For 2 players: opposite corners. For 4 players: 4 corners. For other counts — define a distribution algorithm.
- Starting tile terrain — **⚠️ OPEN QUESTION:** Is the starting tile always Plains, or random? Recommend Plains to give all players a fair start regardless of RNG.

**Starting army:**
- **⚠️ OPEN QUESTION:** Does each player start with a pre-built army, or must they build a Barracks first before they can recruit? This heavily affects early game pacing. Options:
  - A) No starting army — first few turns are pure expansion/building
  - B) 2 Swordsmen on the starting tile — players can immediately contest barbarian tiles

**Starting building:**
- **⚠️ OPEN QUESTION:** Does the starting tile come with a pre-built building (e.g. a Farm), or is it bare? Recommend a pre-built Barracks or Farm to give players immediate agency.

---

## Barbarian Placement

After player starting tiles are set:

1. Identify all remaining unclaimed tiles
2. Randomly select approximately **30%** of them to receive a barbarian army
3. Create barbarian Army records owned by the barbarian kingdom
4. Each barbarian army contains **⚠️ OPEN QUESTION: how many Swordsmen?** Suggested: 2–3 Swordsmen per camp to give early armies a challenge without being impassable.

**Barbarian armies are not placed on tiles adjacent to any player's starting tile** — players need at least one free expansion tile before hitting resistance.

**⚠️ OPEN QUESTION:** Should barbarian army size scale with distance from starting positions? Distant tiles could have larger barbarian forces, making deep expansion more dangerous. Adds strategy but more complexity to implement.

---

## Generation Sequence

1. Create all `Tile` records with Q/R coordinates and randomised `TerrainTypeId`
2. Assign player starting tiles (`HasSettlement = true`, `OwnerKingdomId` = player kingdom)
3. Create barbarian kingdom
4. Place barbarian armies on ~30% of non-starting tiles
5. Initialise `KingdomResource` rows for each player kingdom with starting amounts
6. Set `Game.Status = Active`, `Game.StartedAt = now`
7. Broadcast `GameStarted` SignalR event
