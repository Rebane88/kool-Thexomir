# Systems Reference — Map Generation

> Rules for how the map is generated at game start.

---

## Map Size

Map size is set at lobby creation via `Game.MapWidth` and `Game.MapHeight`.

| Players | Size | Total Tiles |
|---------|------|-------------|
| 2 | 16 × 16 | 256 |
| 3 | 20 × 20 | 400 |
| 4 | 24 × 24 | 576 |

Max players is 4 (see D-27). Maps scale so players have room to expand before borders meet.

---

## Coordinate System

Tiles use **hex axial coordinates** (Q, R). Q + R combination must be unique per game.

Axial neighbors for any tile at (Q, R):
```
(Q+1, R), (Q-1, R), (Q, R+1), (Q, R-1), (Q+1, R-1), (Q-1, R+1)
```

This formula is used consistently everywhere: territory expansion, border detection, attack validation.

---

## Terrain Distribution

Terrain is randomly assigned per tile at generation time using weighted probability.

Weights (admin-editable):

| Terrain | Weight |
|---------|-----------------|
| Plains | 35% |
| Forest | 25% |
| Mountain | 20% |
| Desert | 12% |
| Magic Grove | 8% |

### Anti-Clustering Rule

Terrain is generated randomly but with a spread constraint: a tile cannot be placed if more than **2 of its 6 neighbors** are already the same terrain type. If violated, reroll until a valid terrain is assigned.

### Starting Area Variety

After generation, validate that within **3 hexes of each starting position**, at least **4 of the 5 terrain types** are present. If not, swap tiles to ensure variety. This guarantees every player has meaningful expansion choices in multiple directions.

---

## Player Starting Positions

Each player starts with:
- **1 Castle** on their starting tile
- **6 adjacent tiles** automatically owned (the castle's expansion radius)
- Starting tile terrain is always **Plains** for fairness
- No starting armies — players must build military buildings to train armies

**Placement rules:**
- Players spawn at distributed positions to ensure fair starting distance
- For 2 players: opposite sides of the map
- For 3 players: triangle formation (3 equidistant points around the map edge)
- For 4 players: 4 corners

Starting positions must be on the **edge of the map or within 2–3 tiles of the edge**. Players expand inward, not outward.

Starting positions must ensure no two players' initial 7-tile territories overlap.

---

## Generation Sequence

1. Create all `Tile` records with Q/R coordinates and randomised `TerrainTypeId`
2. Override starting tile terrain to Plains
3. Assign player starting tiles with Castle building
4. Claim 6 adjacent tiles for each player's starting territory
5. Initialise `KingdomResource` rows for each player kingdom with starting amounts
6. Set `Game.Status = Active`, `Game.StartedAt = now`
7. Broadcast `GameStarted` SignalR event
