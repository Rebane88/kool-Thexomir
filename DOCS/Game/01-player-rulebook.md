# Realms of Ash — Player Rulebook

> This document is written for players. No technical implementation details. If you're looking for exact formulas or edge cases, see the Systems Reference documents.

---

## What Is This Game?

Realms of Ash is a turn-based multiplayer strategy game. You control a kingdom on a shared hex map. You expand your territory, build up your economy, recruit armies, and fight to be the last kingdom standing — or the first to meet the win condition.

---

## Getting Started

1. Register an account and log in
2. Create a game lobby or join an existing one
3. Pick your **Faction** (each faction has unique strengths — see Factions section)
4. Wait for the host to start the game (minimum 2 players required)
5. The map generates, each player receives a starting settlement tile
6. Play begins — turns go in order, one player at a time

**You can only be in one active game at a time.**

---

## The Map

The game is played on a hex tile grid. Map size scales with player count. Each tile has a **terrain type** that affects defense and resources. At game start most tiles are unclaimed — you must expand to claim them.

### Terrain Types

| Terrain | Defense Bonus | Movement Cost | Resource Bonus |
|---------|--------------|---------------|----------------|
| Plains | None | 1 | Food |
| Forest | +20% | 2 | Wood |
| Mountain | +40% | 3 | Stone |
| River | +10% | 2 | Gold |
| Magic Grove | +10% | 1 | Mana |

> **Defense Bonus** applies to armies defending on that tile — not to attackers.

---

## Your Turn

Turns are **sequential** — one player acts at a time. When it's your turn you can take any of the following actions (in any order):

### Actions You Can Take

**Claim a Tile**
- The tile must be adjacent to a tile you already own
- Costs Gold
- Cannot claim a tile that has a barbarian army on it — you must defeat them first

**Construct a Building**
- Place a building on one of your owned tiles
- Each tile can hold **one building only**
- Buildings have tier unlock requirements — you must build Tier 1 before Tier 2, on the same tile
- Costs vary by building type

**Recruit Units**
- Add units to an army on one of your tiles
- Requires the appropriate building (e.g. Barracks to recruit Swordsmen)
- Costs resources to recruit and ongoing upkeep each turn

**Move an Army**
- Move one of your armies across your owned tiles
- Movement costs vary by terrain type
- Moving an army onto a tile where you already have an army **merges them**

**Attack**
- Move an army onto an adjacent enemy-owned tile to initiate battle
- Battle resolves automatically — see Combat section
- ⚠️ You cannot undo an attack

### Ending Your Turn
Click **End Turn** when you're done. The income phase runs automatically, then the next player is notified.

---

## Resources

You have 5 resource types. All are earned through buildings and spent on actions.

| Resource | Earned From | Spent On |
|----------|-------------|----------|
| Gold | Markets, Trading Posts, Banks | Claiming tiles, most buildings, most units |
| Food | Farms, Windmills, Granaries | Army upkeep — armies consume Food each turn |
| Wood | Lumber Camps, Sawmills, Timber Halls | Basic buildings |
| Stone | Quarries, Masons, Stoneworks | Advanced buildings and walls |
| Mana | Shrines, Wizard Towers, Arcane Sanctums | Magical units only |

**⚠️ Upkeep Warning:** Every unit costs Food (and sometimes Gold) each turn. If you cannot afford upkeep, your most expensive units are automatically disbanded. Don't overextend your army without the income to support it.

---

## Buildings

Buildings are placed on tiles and produce resources each turn. They come in upgrade chains — you must build each tier in order on the same tile.

### Building Chains

| Chain | Tier 1 | Tier 2 | Tier 3 |
|-------|--------|--------|--------|
| Food | Farm | Windmill | Granary |
| Wood | Lumber Camp | Sawmill | Timber Hall |
| Stone | Quarry | Mason | Stoneworks |
| Gold | Market | Trading Post | Bank |
| Mana | Shrine | Wizard Tower | Arcane Sanctum |
| Military | Barracks | Stables | War Academy |
| Defense | Palisade | Stone Wall | Fortress |

> Military and Defense chains do not produce resources — they unlock unit types and improve tile defense.

**When you capture an enemy tile, any buildings on it transfer to you.**

---

## Units

Units are recruited into armies and used to attack or defend tiles.

| Unit | Strength | Recruited From | Notes |
|------|----------|----------------|-------|
| Swordsman | 10 | Barracks | Reliable all-rounder |
| Archer | 8 | Barracks | Fast to recruit, fragile |
| Knight | 15 | Stables | Powerful, expensive |
| Mage | 12 | Wizard Tower | Costs Mana to recruit and maintain |
| Catapult | 20 | War Academy | Strongest unit, no matchup bonuses |

### Unit Matchups (Rock-Paper-Scissors)

Units deal bonus or reduced damage against specific other unit types:

| Unit | Strong Against | Weak Against |
|------|---------------|--------------|
| Swordsman | Archer, Mage | Knight |
| Archer | Knight | Swordsman, Mage |
| Knight | Mage, Swordsman | Archer |
| Mage | Swordsman | Archer, Knight |
| Catapult | Fortified tiles | Everything else |

---

## Combat

When you move an army onto an enemy tile, a battle occurs automatically.

### How It Works

1. Both sides calculate their **Effective Power** based on unit strengths, matchup bonuses, and terrain
2. A random dice roll (±15%) is applied to both sides
3. The higher power wins
4. The losing side takes casualties; survivors retreat to an adjacent owned tile
5. If the attacker wins, they **capture the tile** and any buildings on it

### Defender Advantage

The defending army benefits from the terrain defense bonus of the tile they're standing on. Mountains (+40%) are very hard to crack. Build defensively in strong terrain.

### Barbarian Camps

Roughly 30% of unclaimed tiles are guarded by barbarian armies. Barbarians only defend — they never attack you. Once you defeat a barbarian army, the tile becomes unclaimed and you can spend Gold to claim it on your next turn.

---

## Factions

Pick your faction when joining the lobby. Each faction has unique passive bonuses. **No two players in the same game can pick the same faction.**

| Faction | Strength | Weakness | Starting Bonus |
|---------|----------|----------|----------------|
| Iron Throne | +20% all unit strength | Buildings cost +10% more | +50 Gold |
| Mage Council | +20% Mana production, +20% Mage strength | — | +30 Mana |
| Merchant Republic | +30% Gold production | Units are -10% weaker | +100 Gold |
| Forest Elves | +20% Food & Wood production | — | +50 Wood |

---

## Win Conditions

The host chooses one win condition when creating the lobby:

**Domination** — First player to own a set percentage of the map wins.

**Elimination** — Last kingdom with tiles remaining wins. You are eliminated when you lose your last tile.

**Score** — After a set number of turns, the player with the highest score wins. Score is calculated from military strength, economic output, and territory size.

---

## Being Defeated

You are defeated when you lose your last owned tile. Once defeated you can no longer take turns. Spectator mode — *coming soon.*

---

## Tips for New Players

- **Expand early** — unclaimed tiles are free (just Gold). Don't let opponents box you in.
- **Match terrain to purpose** — build Stone production on Mountain tiles, Gold on River tiles.
- **Don't neglect Food** — a large army with no Food income will disband itself automatically.
- **Counter your opponent's units** — Archers beat Knights. Swords beat Archers. Know the matchups.
- **Defend in mountains** — a small army on a Mountain tile can hold off a much larger force.
