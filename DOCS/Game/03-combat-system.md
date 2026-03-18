# Systems Reference — Combat System

> Exact rules for how battles resolve. Reference this when implementing attack endpoints and battle record creation.

---

## When Does Combat Trigger?

Combat triggers when a player moves an army onto an **adjacent enemy-owned tile** during their Action Phase. It resolves immediately (not batched).

Combat also triggers when a player moves an army onto a tile containing a **barbarian army**.

---

## Combat Formula

```
EffectivePower = Σ(unit.BaseStrength × matchupModifier) × terrainBonus × diceRoll
```

This is calculated separately for attacker and defender.

### Step 1 — Unit Power with Matchup Modifier

For each attacking unit, find its matchup against each defending unit type:

```
unitContribution = unit.BaseStrength × matchupModifier
```

Where `matchupModifier` comes from `UnitTypeMatchup` table:
- Look up row where `AttackerUnitTypeId = this unit's type` AND `DefenderUnitTypeId = opposing unit type`
- If no row exists, default multiplier = **1.0**
- Strong matchup = **1.25**, weak matchup = **0.75**

**⚠️ OPEN QUESTION:** When an army has mixed unit types fighting a mixed defending army, how is matchup calculated? Options:
- A) Each attacker unit is compared against the most common defender unit type
- B) Each attacker unit is compared against the average matchup across all defender types
- C) Overall army matchup is calculated as an average of all attacker-vs-defender pairs
- **Decision needed before implementing combat.**

### Step 2 — Sum Unit Contributions

```
totalPower = Σ all unit contributions
```

### Step 3 — Apply Terrain Bonus (Defender Only)

```
defenderPower = totalPower × (1 + TerrainType.DefenseBonus)
```

Attacker receives no terrain bonus.

**⚠️ OPEN QUESTION:** Does the attacker's terrain (the tile they're attacking from) have any effect? Currently: no. Confirm this is intentional.

### Step 4 — Apply Faction Unit Bonus

```
power = power × FactionType.UnitStrengthBonus
```

Apply only if `FactionType.BonusUnitType` matches unit type, or if `BonusUnitType = null` (applies to all).

**⚠️ OPEN QUESTION:** Does the faction unit bonus apply before or after terrain bonus? Define the exact multiplication order.

### Step 5 — Dice Roll

```
finalPower = power × diceRoll
```

Where `diceRoll` = random decimal between **0.85 and 1.15** (uniform distribution).

Both attacker and defender roll independently.

---

## Outcome Determination

```
if AttackerFinalPower > DefenderFinalPower → AttackerWon
if DefenderFinalPower >= AttackerFinalPower → DefenderWon
```

**⚠️ OPEN QUESTION:** What happens on an exact tie? Currently specced as defender wins (>= defender). Confirm.

---

## Casualties

After outcome is determined:

**Losing side casualties:**
```
casualtyRate = winnerPower / loserPower  (capped at some max)
unitsLost = ceil(loserUnitCount × casualtyRate)
```

**⚠️ OPEN QUESTION:** Exact casualty formula is not defined. Options:
- A) Losing side loses a % of units proportional to power difference
- B) Losing side always loses all units
- C) Both sides take casualties proportional to each other's power

**⚠️ OPEN QUESTION:** Does the winning side take any casualties? Currently implied no — confirm.

Units are **hard deleted** from the database when killed. Counts are stored on the `Battle` record (`AttackerUnitsLost`, `DefenderUnitsLost`).

---

## Tile Ownership Change

If `Outcome = AttackerWon`:
- `Tile.OwnerKingdomId` → AttackerKingdomId
- `Battle.TileChangedOwner = true`
- Buildings on the tile remain — they transfer to the attacker

If `Outcome = DefenderWon`:
- No ownership change
- `Battle.TileChangedOwner = false`

---

## Army After Battle

**Attacker wins:**
- Attacker's surviving army moves onto the captured tile
- Defender's surviving units retreat to an adjacent tile owned by the defender

**Attacker loses:**
- Attacker's surviving units retreat to the tile they attacked from
- Defender's army remains on the tile

**⚠️ OPEN QUESTION:** Retreat edge cases — see turn-structure.md Phase 3 for the open questions on retreat with no valid destination tile.

**⚠️ OPEN QUESTION:** If attacker wins and their army moves to the captured tile, and that tile already has another attacker army — do they merge? (Per Army rules, same kingdom + same tile = merge. Apply here too.)

---

## Barbarian Combat

Barbarian armies use standard Swordsman units. Combat resolves identically to PvP combat using the same formula.

If barbarian army is defeated:
- `Tile.OwnerKingdomId` remains **null** (does not transfer to attacker automatically)
- Tile is now unclaimed — attacker may spend Gold to claim it on a future action

If barbarian army wins:
- Attacker takes casualties, retreats
- Barbarian army remains on tile

---

## Battle Record

Every battle creates a `Battle` row with:

| Field | Value |
|-------|-------|
| GameId | Current game |
| TurnNumber | Current turn |
| AttackerKingdomId | Attacking kingdom |
| DefenderKingdomId | Defending kingdom (or barbarian kingdom) |
| TileId | Contested tile |
| AttackerPowerTotal | Final calculated attacker power (after all modifiers, before dice) |
| DefenderPowerTotal | Final calculated defender power (after all modifiers, before dice) |
| AttackerDiceRoll | The random roll applied to attacker (0.85–1.15) |
| DefenderDiceRoll | The random roll applied to defender (0.85–1.15) |
| AttackerUnitsLost | Count of attacker units killed |
| DefenderUnitsLost | Count of defender units killed |
| Outcome | AttackerWon or DefenderWon |
| TileChangedOwner | Bool |
| OccurredAt | Server timestamp |

A `TurnLog` entry with `EventType = BattleOccurred` is also created, with `Metadata` containing the `battleId`.
