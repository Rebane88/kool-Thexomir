# Systems Reference — Turn Structure

> This document defines the exact sequence of events within a single game round. A round consists of all players taking their action phase, followed by a shared combat phase, then income.

---

## Round Phases (In Order)

```
1. Action Phase         ← each player takes their turn sequentially
2. Battle Phase         ← army selection → lineup order → resolve → playback
3. Income Phase         ← resources generated, upkeep paid for ALL players
4. Round End            ← win conditions checked, round number advances
```

---

## Phase 1 — Action Phase (Sequential, Per Player)

Each player takes their turn one at a time. On your turn, you have a pool of **action points** to spend. The base action count is configurable (admin-editable). Future mechanics may modify a player's action count (buildings, factions, etc.).

Each turn has a **time limit** (configurable per game lobby). If the timer expires, the turn auto-ends and unspent actions are lost.

### Available Actions (Each Costs 1 Action Unless Noted)

| Action | Description | Validation |
|--------|-------------|------------|
| Build | Place a tier 1 building on an owned empty tile | Tile must be owned, no existing building, have resources |
| Upgrade | Upgrade existing building to next tier | Building must exist, next tier must exist, have resources |
| Train Army | Train a new army (added to global roster) | Must have required military building, have resources |
| Declare Attack | Declare an attack on an enemy border tile | Target tile must border your territory, pick your tile to risk (must be adjacent to target) |

### Building Expansion

When a building is placed on a tile, all **adjacent unowned tiles** automatically become part of your kingdom. This is the primary expansion mechanic — armies do not claim territory.

- Only unowned tiles are claimed (never steals from other players)
- Upgrading a building does NOT trigger additional tile claiming

### Declaring an Attack

When declaring an attack, the player must:

1. Select a **target tile** (enemy tile bordering their territory)
2. Select their **risked tile** (own tile adjacent to the target — this is what they lose if they lose the battle)

The player must have **at least 1 army** in their roster to declare an attack. Army selection itself does NOT happen here — it happens in the Battle Phase after all action phases are complete.

The attack is queued — it does not resolve until the Battle Phase.

**Tile Locking:** Once an attack is declared, both the target tile and the risked tile are **locked** for the rest of the round. No other attack can involve either tile (as target or as risk). This prevents conflicting claims on the same tiles.

**Visibility:** All players can see that an attack was declared, which tile is targeted, and which tile is at risk.

### Action Logging

Every action creates a `TurnLog` entry with the appropriate `EventType`, human-readable `Description`, and JSON `Metadata`.

### Turn Timer

Each player's action phase is time-limited (configurable per lobby). When the timer expires:
- Turn auto-ends
- Unspent action points are lost
- Any partially configured attack declarations are cancelled

---

## Phase 2 — Battle Phase (All Players Involved in Battles)

If no attacks were declared this round, this phase is skipped entirely. Otherwise it proceeds in 4 steps:

### Step 1 — Army Selection (Simultaneous, Hidden)

All players involved in battles select their armies at the same time:

- For each battle, both attacker and defender pick armies from their global roster:
  - Up to **3 armies** for standard battles
  - Up to **5 armies** if either the target or risked tile contains a castle
- Players can commit fewer armies than the max if they don't have enough (even 1 is valid)
- If a player has **0 available armies**, they enter the battle with nothing and **automatically lose** (combat ends immediately when one side has 0 armies)
- Each army can only be committed to **one battle per round**
- Selections are **hidden** from the opponent during this step
- **Time limit** applies (configurable). If time expires, armies are auto-selected (strongest first by Attack stat)

### Step 2 — Army Reveal

Once both sides have selected (or time expires), selections are **revealed** to both sides. Both players can now see:
- Which army types the opponent selected
- Current HP of each opponent army

### Step 3 — Lineup Order (Simultaneous, Hidden)

Both sides set the **order** of their committed armies (which army fights first, second, third, etc.):

- Lineups are set **simultaneously** — order is hidden from the opponent
- **Time limit** applies (configurable, D-36: 20s base + 5s per battle). If time expires, order defaults to the order armies were selected

### Step 4 — Combat Resolution & Playback

All battles are resolved server-side instantly once lineups are locked. Results are then played back one battle at a time for all players to watch.

**Resolution:** For each battle, the round-by-round combat system resolves (see combat-system.md):

1. Each side's first army in the lineup enters the fight
2. Rounds play out: initiative roll → damage roll → chip damage
3. When an army dies, the next in lineup replaces it
4. Combat ends when one side has no armies left
5. If both sides' last armies die in the same round, the side that won the initiative roll that round wins

**Battle Results:**

- **Attacker wins:** Attacker gains the defender's targeted tile. Building on captured tile is **destroyed**. Attacker keeps their risked tile.
- **Defender wins:** Defender gains the attacker's risked tile. Building on taken tile is **destroyed**. Defender keeps their targeted tile.

**Post-battle army state:**
- Armies that died during combat are permanently destroyed (removed from roster)
- Surviving armies return to the player's global roster with their current (damaged) HP — they heal during Income Phase Step 3

**Battle Playback:**
- Battles play back one at a time for ALL players to watch (no skipping)
- Playback order does not affect outcomes (all battles are pre-resolved)
- Each round shows: armies involved, initiative roll result, damage dealt, chip damage dealt, HP remaining

Players not involved in any battles wait during this phase.

---

## Phase 3 — Income Phase (Simultaneous, All Players)

Runs automatically after the combat phase. Processes for ALL kingdoms at the same time in this order:

### Step 1 — Resource Generation

For each kingdom, for each owned tile with a building:
```
yield = BuildingType.BaseYield
      × TerrainType.ResourceMultiplier (if terrain matches BonusResourceType)
      × FactionType.ResourceProductionModifier (applies to all building yields)
```
Add yield to `KingdomResource.Amount` for the matching resource type.

Tiles without buildings generate no resources.

Newly captured tiles (from this round's combat) are included — they generate income if they still have a building. (Note: captured tiles have their buildings destroyed, so they won't generate income until rebuilt.)

### Step 2 — Army Upkeep

For each army in the kingdom's global roster:
- Deduct army type's upkeep costs from resources

If resources would go below 0:
- Floor at 0
- Disband armies starting from most expensive upkeep first until affordable
- Log each disbanded army

Upkeep values are admin-editable per army type (can be set to 0).

### Step 3 — Army Healing

All armies in every kingdom's roster heal a flat percentage of their max HP:

```
army.currentHP = min(army.currentHP + (army.maxHP × healPercent), army.maxHP)
```

The heal percentage is a global game setting (admin-editable). Suggested starting value: 5–10% per round.

This applies to all surviving armies in the roster — including those that fought this round. Dead armies (0 HP) are permanently destroyed during combat and removed from the roster before this phase runs. They cannot be healed.

### Step 4 — Win Condition Check

After all income is processed:
- Check if any kingdom meets the active win condition (see win-conditions.md)
- Castle destruction during combat is checked — if a kingdom lost their castle tile, they are **eliminated**
- If a kingdom is eliminated: set `Kingdom.Status = Defeated`, broadcast `KingdomDefeated` event
- If game is won: set `Game.Status = Finished`, broadcast `GameOver` event

---

## Phase 4 — Round End

- Advance `Game.RoundNumber` by 1
- Reset all per-round flags (armies available for battle, etc.)
- Create `TurnLog` entry with `EventType = RoundEnded`
- Begin Phase 1 for the next round

---

## Terminology

| Term | Meaning |
|------|---------|
| **Round** | One full cycle of all phases (action → battle → income → end) |
| **Turn** | One player's action phase within a round |
| **Action Point** | Currency spent to perform actions during a turn |
| **Battle** | A single combat encounter between two players' army pools |
| **Lineup** | The order a player assigns to their armies for a battle |

---

## TurnLog EventTypes Reference

| EventType | When Created |
|-----------|-------------|
| RoundStarted | Beginning of Phase 1 |
| TurnStarted | When a player's action phase begins |
| BuildingConstructed | Action Phase — build |
| BuildingUpgraded | Action Phase — upgrade |
| ArmyTrained | Action Phase — train |
| AttackDeclared | Action Phase — declare attack |
| ArmySelected | Battle Phase Step 1 — army selection |
| LineupSet | Battle Phase Step 3 — army order set |
| BattleResolved | Battle Phase Step 4 — battle result |
| TileCaptured | Battle Phase Step 4 — tile ownership changed |
| BuildingDestroyed | Battle Phase Step 4 — building on captured tile razed |
| ResourcesEarned | Income Phase Step 1 |
| UpkeepPaid | Income Phase Step 2 |
| ArmyDisbanded | Income Phase Step 2 — upkeep failure |
| ArmyHealed | Income Phase Step 3 |
| KingdomDefeated | Income Phase Step 4 |
| GameOver | Income Phase Step 4 — win condition met |
| RoundEnded | Phase 4 |
