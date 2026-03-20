# Realms of Ash — Core Game Design Document

## Overview

A turn-based fantasy medieval kingdom builder. Players each manage their own kingdom on a shared hex tile map. They expand territory by building, grow their economy, train armies, and fight to be the last kingdom standing.

Max **4 players** per game. Win condition: **Elimination** (lose your Castle = eliminated, last standing wins).

> For detailed rules on each system, see the documents in `DOCS/Game/`. This file is a high-level overview only.

---

## Tech Stack

- **Backend:** ASP.NET Core REST API (C#)
- **Frontend:** React + TypeScript
- **Database:** PostgreSQL (via Docker)
- **Real-time:** SignalR (phase change notifications)

---

## Core Game Loop

1. Player logs in and joins or creates a game lobby
2. Game starts when host clicks "Start" (minimum 2 players) — map generates, each player gets a Castle and 7 starting tiles
3. Each round plays out in phases: **Action → Battle → Income → Round End**
4. On your turn (Action Phase) you spend action points to: build, upgrade, train armies, or declare attacks
5. Battles resolve in the Battle Phase — army selection, lineup order, then auto-resolved combat
6. Income Phase generates resources, pays upkeep, heals armies, and checks win conditions
7. Game ends when one kingdom remains or round 100 is reached

---

## Key Systems

| System | Summary | Details |
|--------|---------|---------|
| Turn Structure | Round-based: action → battle → income → end | `02-turn-structure.md` |
| Combat | Round-by-round gambling with pre-set lineups (3v3 / 5v5) | `03-combat-system.md` |
| Resources | 5 types (Gold, Food, Wood, Stone, Mana), building-generated | `04-resource-system.md` |
| Buildings | 6 chains (5 resource + 1 military), territory expansion on placement | `05-building-system.md` |
| Army Types | 6 types with distinct risk profiles, no matchups | `08-army-types.md` |
| Factions | 4 factions with unique strengths and weaknesses | `10-factions.md` |
| Slot Machine | Gold-sink gambling for extra action points | `11-slot-machine.md` |
| Win Conditions | Elimination only — lose castle = out | `06-win-conditions.md` |
| Map Generation | Hex grid, anti-clustering terrain, fair starting positions | `07-map-generation.md` |

---

## Terrain Types

| Terrain | Bonus Resource | Yield Multiplier |
|---------|---------------|-----------------|
| Plains | Food | 1.10x |
| Forest | Wood | 1.10x |
| Mountain | Stone | 1.10x |
| Desert | Gold | 1.10x |
| Magic Grove | Mana | 1.10x |

Terrain affects building yield only — armies are global and not positioned on the map.

---

## Buildings

6 upgrade chains (each tier replaces the previous on the same tile):

| Chain | Tier 1 | Tier 2 | Tier 3 |
|-------|--------|--------|--------|
| Food | Farm | Windmill | Granary |
| Wood | Lumber Camp | Sawmill | Timber Hall |
| Stone | Quarry | Mason | Stoneworks |
| Gold | Market | Trading Post | Bank |
| Mana | Shrine | Wizard Tower | Arcane Sanctum |
| Military | Barracks | Stables | War Academy |

Placing a tier 1 building claims all adjacent unowned tiles. The Castle produces +10 Food, Wood, Stone per round.

---

## Army Types

| Type | Attack | HP | Initiative | Required Building |
|------|--------|----|-----------|-------------------|
| Warrior | 25 | 100 | 50 | Barracks |
| Scout | 15 | 60 | 70 | Barracks |
| Knight | 35 | 140 | 30 | Stables |
| Berserker | 35 | 60 | 50 | Stables |
| Mage | 35 | 60 | 70 | War Academy |
| Guardian | 15 | 140 | 30 | War Academy |

No rock-paper-scissors matchups. Combat is determined by stats, dice, and lineup order.

---

## Factions

| Faction | Identity | Strength | Weakness | Starting Bonus |
|---------|----------|----------|----------|----------------|
| Iron Throne | Warmonger | +15% Attack, +50% chip damage | -15% resource production | +50 Gold |
| Mage Council | Strategist | +20% Initiative, +1 action | -15% army HP | +30 Mana |
| Merchant Republic | Economist | -20% building costs, -15% training costs | -10% Attack | +100 Gold |
| Forest Elves | Survivor | +15% HP, +50% heal rate | -1 action per turn | +50 Wood |

---

## Real-Time Notifications (SignalR)

SignalR is used **only for notifications** — all actual game data flows through the REST API.

| Event | Triggered When |
|-------|---------------|
| `PhaseChanged` | Game transitions between phases |
| `GameStarted` | Lobby starts and game begins |
| `KingdomDefeated` | A player's castle is destroyed |
| `GameOver` | Win condition is met or round 100 reached |

---

## Multiplayer & Lobby

- Players register and log in via ASP.NET Core Identity
- Any player can create a game lobby with configurable settings (map size, max players, turn time limit)
- Other players join via lobby code or public lobby list
- Game starts when host clicks "Start" (minimum 2 players, maximum 4)
- A user can only be in one active game at a time

---

## Roles & Admin Panel

### Roles (ASP.NET Core Identity)

| Role | Assigned To | Can Do |
|------|-------------|--------|
| `Admin` | Server owner, manually assigned | Full access to admin panel — edit all reference data, manage users |
| `Player` | Everyone on registration | Create/join games, play |

### Admin Panel

Admins can edit all game balance values live via the UI:
- `TerrainType` — resource multipliers, map colors
- `BuildingType` — base yields, costs, unlock tree
- `ArmyType` — stats, costs, situational bonuses
- `FactionType` — all faction modifiers and starting bonuses
- `AppUser` — manage user accounts and role assignments

---

## Entity List

1. `AppUser` — player account (extends IdentityUser)
2. `Game` — a match instance (status, round number, settings, lobby phase)
3. `Kingdom` — a player's realm within a game
4. `Tile` — individual hex cell (coordinates, terrain type, owner)
5. `TerrainType` — terrain definitions (reference, admin editable)
6. `BuildingType` — building template with tier, unlock tree, costs, yields (reference, admin editable)
7. `Building` — a building instance placed on a tile
8. `ArmyType` — army template with stats and costs (reference, admin editable)
9. `Army` — single army entity in a kingdom's global roster
10. `Battle` — record of combat between two sides
11. `BattleRound` — individual round within a battle
12. `KingdomResource` — current resource amounts per kingdom (5 rows per kingdom)
13. `TurnLog` — full history of every action and event per turn
14. `FactionType` — playable faction with passive modifiers (reference, admin editable)
