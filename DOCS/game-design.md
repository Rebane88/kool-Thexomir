# Realms of Ash — Core Game Design Document

## Overview

A turn-based fantasy medieval kingdom builder. Players each manage their own kingdom on a shared hex tile map. They expand territory, construct and upgrade buildings, recruit armies, and compete to dominate the world through conquest and strategy.

---

## Tech Stack

- **Backend:** ASP.NET Core REST API (C#)
- **Frontend:** React + TypeScript
- **Database:** PostgreSQL (via Docker)
- **Real-time:** SignalR (turn change notifications)

---

## Core Game Loop

1. Player logs in and joins or creates a game lobby
2. Game starts when enough players are ready — map generates, each player gets a starting tile with a basic settlement
3. Players take **sequential turns** — one player acts at a time
4. On your turn you can perform actions (claim tiles, build, recruit, move armies, attack)
5. End turn — income phase runs, resources are generated, next player is notified via SignalR
6. Game ends when a win condition is met

---

## Turn Structure

### Action Phase (current player only)
- Claim adjacent unclaimed tiles (costs Gold)
- Construct or upgrade a building on an owned tile (costs resources)
- Recruit units into an army (costs resources)
- Move armies across owned tiles
- Attack an adjacent enemy-owned tile

### Resolution Phase
- Battles resolve automatically using the combat formula

### Income Phase
- Resources generated based on owned buildings and terrain bonuses
- Food consumption checked against army size

### End Turn
- Turn passes to the next player
- SignalR notifies the next player's client

---

## Resources

| Resource | Primary Source | Used For |
|----------|---------------|----------|
| Gold | Markets, Trade Routes | Claiming tiles, building, recruiting |
| Food | Farms | Army upkeep, population growth |
| Wood | Lumber Camps | Basic buildings |
| Stone | Quarries | Advanced buildings, walls |
| Mana | Shrines / Wizard Towers | Recruiting magical units only |

---

## Terrain Types

| Terrain | Defense Bonus | Movement Cost | Resource Bonus |
|---------|--------------|---------------|----------------|
| Plains | 0% | 1 | +Food |
| Forest | +20% | 2 | +Wood |
| Mountain | +40% | 3 | +Stone |
| River | +10% | 2 | +Gold |
| Magic Grove | +10% | 1 | +Mana |

---

## Buildings

Buildings are placed on owned tiles. Each building can be upgraded through tiers, and some buildings unlock others.

### Food Chain
```
Farm → Windmill → Granary
```

### Wood Chain
```
Lumber Camp → Sawmill → Timber Hall
```

### Stone Chain
```
Quarry → Mason → Stoneworks
```

### Gold Chain
```
Market → Trading Post → Bank
```

### Mana Chain
```
Shrine → Wizard Tower → Arcane Sanctum
```

### Military Chain
```
Barracks → Stables → War Academy
```

### Defense Chain
```
Palisade → Stone Wall → Fortress
```

Each tier costs more resources but produces more output or unlocks new unit types.

---

## Units

### Unit Type Matchups (Rock-Paper-Scissors)

| Unit | Strong Against | Weak Against | Unlocked By |
|------|---------------|--------------|-------------|
| Swordsman | Archer | Knight | Barracks |
| Archer | Knight | Mage | Barracks |
| Knight | Mage | Swordsman | Stables |
| Mage | Swordsman | Archer | Wizard Tower |
| Catapult | Fortified tiles | All units | War Academy |

### Magical Units (require Mana to recruit and maintain)
- **Mage** — high damage, fragile
- More unlocked via Arcane Sanctum tier

---

## Combat System

When an army attacks an adjacent enemy tile, battle resolves automatically:

### Combat Formula
```
Effective Power = Σ(unit base strength × matchup modifier × terrain bonus) × dice roll
```

- **Matchup modifier:** +25% if strong against opponent unit type, -25% if weak
- **Terrain bonus:** defender gets terrain defense bonus (e.g. +40% in mountains)
- **Dice roll:** random multiplier between 0.85 and 1.15 (adds unpredictability)

### Outcome
- Higher effective power wins
- Losing side takes proportional casualties
- If attacker wins, they capture the tile and its buildings
- Defender's remaining units retreat to an adjacent owned tile

---

## Map

- **Hex tile grid** (e.g. 20×20 for a 4-player game, scales with player count)
- Map generates at game start with randomized terrain distribution
- Each player spawns at a corner/edge with one starting settlement tile
- Unclaimed tiles are neutral — no defense bonus, free to claim

---

## Factions

Each player picks a unique faction when joining a lobby. No two players in the same game can pick the same faction. Factions apply passive modifiers throughout the game. Admins can tune all values via the admin panel.

| Faction | Flavour | Resource Bonus | Unit Bonus | Building Cost | Starting Bonus |
|---------|---------|---------------|------------|---------------|----------------|
| **Iron Throne** | Military powerhouse | — | +20% all units | +10% | +50 Gold |
| **Mage Council** | Magic and knowledge | +20% Mana | +20% Mages only | -10% | +30 Mana |
| **Merchant Republic** | Economic dominance | +30% Gold | -10% all units | -20% | +100 Gold |
| **Forest Elves** | Nature and speed | +20% Food & Wood | — | -10% | +50 Wood |

---

## Barbarian Camps

A special neutral barbarian kingdom is created per game at map generation. Approximately 30% of unclaimed tiles are assigned a small barbarian army. Players must defeat the barbarian army to claim the tile — they cannot simply spend gold to claim guarded tiles.

**Rules:**
- Barbarians only defend — they never move or attack players
- Barbarians do not respawn once defeated
- After defeating a barbarian army, the tile becomes unclaimed and can be claimed normally next turn
- Barbarian armies use Swordsman units with standard stats
- Reuses the existing combat system — no special logic needed

---

Configurable per game lobby:

| Condition | Description |
|-----------|-------------|
| **Domination** | First to own X% of the map wins |
| **Elimination** | Last kingdom standing wins |
| **Score** | Highest score after N turns wins (military + economic + territory) |

---

## Real-Time Notifications (SignalR)

SignalR is used **only for notifications** — all actual game data flows through the REST API.

### Events
| Event | Triggered When |
|-------|---------------|
| `TurnChanged` | Current player ends their turn — notifies next player |
| `GameStarted` | Lobby is full and game begins |
| `KingdomDefeated` | A player's last tile is captured |
| `GameOver` | Win condition is met |

### Flow
```
Player A clicks "End Turn"
  → POST /api/game/{id}/end-turn
  → Server processes income phase
  → SignalR broadcasts TurnChanged to Player B
  → Player B's React client fetches latest game state
  → Player B's UI unlocks for their turn
```

---

## Multiplayer & Lobby

- Players register and log in via ASP.NET Core Identity
- Any player can create a game lobby with configurable settings (map size, win condition, max players, turn time limit)
- Other players join via lobby code or public lobby list
- Game starts when host clicks "Start" (minimum 2 players)
- A user can only be in one active game at a time

---

## Roles & Admin Panel

### Roles (ASP.NET Core Identity)

| Role | Assigned To | Can Do |
|------|-------------|--------|
| `Admin` | Server owner, manually assigned | Full access to admin panel — edit all reference data, manage users |
| `Player` | Everyone on registration | Create/join games, play |

### Admin Panel

Admins have access to a dedicated admin panel to manage and tune the game without touching code. The admin panel is built from controllers in ASP.NET MVC and secured behind `[Authorize(Roles = "Admin")]`.

**Admins can edit:**
- `TerrainType` — defense bonuses, movement costs, resource multipliers, map colors, icon URLs
- `BuildingType` — base yields, costs, unlock tree, icon URLs, descriptions
- `UnitType` — base strength, recruit costs, upkeep costs, icon URLs, descriptions
- `UnitTypeMatchup` — damage multipliers (balance tuning)
- `GameEvent` — event types and resource effects
- `AppUser` — manage user accounts and role assignments

This means game balance (unit strength, building costs, terrain bonuses) and all visual assets (icon URLs) can be updated live via the UI with no code deployment needed.

---

## Entity List

1. `AppUser` — player account (extends IdentityUser)
2. `Game` — a match instance (status, turn number, win condition, settings, lobby phase)
3. `Kingdom` — a player's realm within a game (also used for barbarian camp)
4. `Tile` — individual hex cell (coordinates, terrain type, owner)
5. `TerrainType` — plains, forest, mountain, river, magic grove (reference, admin editable)
6. `BuildingType` — building template with tier, unlock tree, costs, yields (reference, admin editable)
7. `Building` — a building instance placed on a tile
8. `UnitType` — unit template with stats and costs (reference, admin editable)
9. `UnitTypeMatchup` — combat matchup multipliers (reference, admin editable)
10. `Army` — a group of units positioned on a tile
11. `Unit` — individual unit belonging to an army
12. `Battle` — record of combat between two armies
13. `KingdomResource` — current resource amounts per kingdom (5 rows per kingdom)
14. `TurnLog` — full history of every action and event per turn
15. `GameEvent` — random events affecting kingdoms or tiles
16. `FactionType` — playable faction with passive stat modifiers (reference, admin editable)

---

## Open Questions / Future Features

- Random events system (dragon attacks, plague, magic storms)
- Observer mode for finished players
- Replay system (rewatch a completed game turn by turn)
