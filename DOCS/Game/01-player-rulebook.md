# Realms of Ash — Player Rulebook

> This document is written for players. No technical implementation details. If you're looking for exact formulas or edge cases, see the Systems Reference documents.

---

## What Is This Game?

Realms of Ash is a turn-based multiplayer strategy game. You control a kingdom on a shared hex map. Expand your territory by building, grow your economy, train armies, and fight to be the last kingdom standing — or the first to meet the win condition.

---

## Getting Started

1. Register an account and log in
2. Create a game lobby or join an existing one
3. Pick your **Faction** (each faction has unique strengths — see Factions section)
4. Wait for the host to start the game (minimum 2 players required)
5. The map generates — each player starts with a Castle and 7 tiles of territory
6. Play begins in rounds

**You can only be in one active game at a time.**

---

## The Map

The game is played on a hex tile grid. Map size scales with player count. Each tile has a **terrain type** that affects building yield.

### Terrain Types

| Terrain | Resource Bonus |
|---------|---------------|
| Plains | Food |
| Forest | Wood |
| Mountain | Stone |
| Desert | Gold |
| Magic Grove | Mana |

> Build resource buildings on matching terrain for bonus yield. You can build any building on any terrain — matching just gives a bonus.

---

## Game Rounds

Each round has phases that play out in order:

### 1. Action Phase (Your Turn)

Players take turns one at a time. On your turn you have **action points** to spend. Each action costs 1 point:

**Build** — Place a tier 1 building on an empty tile you own. This also **claims all adjacent unowned tiles** for your kingdom.

**Upgrade** — Upgrade an existing building to the next tier (e.g. Farm → Windmill → Granary). Same tile, better output.

**Train Army** — Train a new army from a military building. The army joins your global roster.

**Declare Attack** — Target an enemy border tile to attack. You must also pick one of your own border tiles to risk — if you lose, your opponent takes it. You don't pick armies yet — that happens in the Battle Phase.

Your turn is timed. When time runs out, your turn ends and unspent actions are lost.

### 2. Battle Phase

If any battles were declared this round:

1. **Pick Armies** — Both attacker and defender select armies from their roster simultaneously (3 for standard battles, 5 if a Castle is involved). Picks are hidden.
2. **Armies Revealed** — Both sides see what the opponent picked (types and HP).
3. **Set Order** — Both sides arrange their armies in fighting order. Order is hidden from the opponent.
4. **Battles Resolve** — All battles play out automatically and play back one at a time for everyone to watch. See the Combat section for details.

### 3. Income Phase

All players simultaneously earn resources from their buildings, pay army upkeep, and armies heal a percentage of max HP.

---

## Resources

You have 5 resource types. All are earned through buildings and spent on actions.

| Resource | Earned From | Spent On |
|----------|-------------|----------|
| Gold | Markets, Trading Posts, Banks | Most buildings, most armies |
| Food | Farms, Windmills, Granaries | Army upkeep |
| Wood | Lumber Camps, Sawmills, Timber Halls | Basic buildings |
| Stone | Quarries, Masons, Stoneworks | Advanced buildings |
| Mana | Shrines, Wizard Towers, Arcane Sanctums | Magical armies only |

**Upkeep Warning:** Every army costs resources each turn. If you can't afford upkeep, your most expensive armies are automatically disbanded. Don't overextend without the income to support it.

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

> Military buildings don't produce resources — they unlock army types for training.

**When you capture an enemy tile through combat, the building on it is destroyed.** You'll need to rebuild.

### The Castle

Your Castle is the heart of your kingdom. **If you lose your Castle, you are eliminated.** Protect it at all costs. Castle battles are larger — 5 armies per side instead of 3.

---

## Armies

Armies are single units you train from military buildings. Each army type has different stats:

- **Attack** — how much damage it deals
- **HP** — how much damage it takes before dying
- **Initiative** — chance of striking first in combat rounds

Armies are part of your **global roster** — they aren't positioned on the map. You assign them to battles during the Battle Phase.

**Damaged armies stay damaged** between battles, but all armies heal a small percentage of their max HP each round during the Income Phase.

---

## Combat

During your turn, you declare an attack by picking a target enemy tile and risking one of your own tiles. In the Battle Phase, both sides pick up to 3 armies (up to 5 if a Castle is involved), see each other's picks, then set their lineup order. You can fight with fewer armies if that's all you have — but if you have none, you lose automatically.

### How It Works

1. Both sides pick armies (up to 3, or 5 for castle battles), then set lineup order (hidden from opponent)
2. The first army from each lineup faces off
3. **Initiative Roll** — based on both armies' Initiative stats, one side wins the right to strike
4. **Damage Roll** — the winner deals a percentage of their Attack as damage to the loser's army
5. **Chip Damage** — the loser deals a small amount of damage back
6. This repeats each round. When an army dies, the next in your lineup steps up
7. Combat ends when one side has no armies left

### Stakes

- **If you win:** you capture the enemy's tile (building on it is destroyed)
- **If you lose:** your opponent captures the tile you risked (building on it is destroyed)

**Every attack is a gamble.** Your odds are better with stronger armies, but the dice can always turn.

### Castle Battles

When a Castle tile is involved (either attacking or defending), the battle size increases to **5 armies per side**.

---

## Factions

Pick your faction when joining the lobby. Each faction has unique passive bonuses. **No two players in the same game can pick the same faction.**

| Faction | Strength | Weakness | Starting Bonus |
|---------|----------|----------|----------------|
| Iron Throne | +15% army Attack, +50% chip damage | -15% resource production | +50 Gold |
| Mage Council | +20% army Initiative, +1 action per turn | -15% army HP | +30 Mana |
| Merchant Republic | -20% building costs, -15% training costs | -10% army Attack | +100 Gold |
| Forest Elves | +15% army HP, +50% heal rate | -1 action per turn | +50 Wood |

---

## Win Conditions

**Elimination** — Last kingdom standing wins. You are eliminated when you lose your Castle.

---

## Being Defeated

You are eliminated when your Castle is destroyed. Once defeated you can no longer take turns.

---

## Tips for New Players

- **Build early** — every building claims adjacent tiles. Expand before your opponents box you in.
- **Match terrain to buildings** — build Food on Plains, Stone on Mountains for bonus yield.
- **Don't neglect Food** — a large army roster with no Food income will disband itself.
- **Balance economy and military** — all buildings and no armies means you can't defend. All armies and no economy means you can't sustain.
- **Choose your battles** — every attack risks one of YOUR tiles too. Make sure the odds are in your favour.
- **Protect your Castle** — lose it and you're out. Keep it away from borders if possible.
