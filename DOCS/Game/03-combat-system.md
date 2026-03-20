# Systems Reference — Combat System

> Round-by-round gambling-style combat. Players set army lineups, dice determine who strikes each round. All strategy is in the preparation — combat resolves automatically.

---

## Overview

Combat is a multi-round auto-resolved battle between two army pools (3v3 standard, 5v5 if a castle is involved). Players commit armies and set their lineup order before combat begins. The server resolves all rounds using initiative and damage rolls, then plays back the results for all players to watch.

---

## Army Stats

Each army is a single entity (not a squad). Every army type has the following stats, all **admin-editable**:

| Stat | Purpose | Example |
|------|---------|---------|
| Attack | Base damage dealt when winning a round | 30 |
| HP | Health pool — destroyed at 0 | 100 |
| Initiative | Chance weight for winning the round roll | 60 |
| Damage Range Min (%) | Minimum % of effective Attack dealt on hit | 60% |
| Damage Range Max (%) | Maximum % of effective Attack dealt on hit | 100% |
| Chip Damage Range Min (%) | Minimum % of loser's effective Attack dealt back | 0% |
| Chip Damage Range Max (%) | Maximum % of loser's effective Attack dealt back | 20% |
| Upkeep Cost | Per-round resource cost to maintain | 5 Gold, 3 Food |
| Training Cost | One-time resource cost to train | 50 Gold, 20 Food |
| Required Building | Military building tier needed to train this type | Stables |

### HP-to-Damage Scaling

An army's effective Attack scales linearly with its current HP:

```
effectiveAttack = baseAttack × (currentHP / maxHP)
```

A damaged army deals less damage. **Initiative does NOT degrade with HP** — it stays constant.

### Situational Bonuses

Army types can define situational bonuses (all admin-editable per army type):

- **Defending bonus** — e.g. Warrior: +10% Attack when defending
- **Attacking bonus** — e.g. Berserker: +15% Attack when attacking

These modify Attack (or Initiative, e.g. Guardian: +15% Initiative when defending) based on the player's role in the battle (attacker or defender). See `08-army-types.md` for all situational bonuses.

### Faction Bonuses

Faction modifiers apply to army stats globally for all armies owned by that faction. For example:
- Iron Throne: +15% Attack on all armies
- Mage Council: +20% Initiative on all armies

Applied on top of base stats and situational bonuses. See `10-factions.md` for full details. All admin-editable.

---

## Combat Setup

### Declaring an Attack (During Action Phase)

1. Player spends 1 action point to declare an attack
2. Selects a **target tile** (enemy tile bordering their territory)
3. Selects their **risked tile** (own tile adjacent to the target — this is what they lose if they lose the battle)

Army selection does NOT happen during the Action Phase. Only the tiles are chosen.

**Tile Locking:** Both the target tile and the risked tile are **locked** for the round once an attack is declared. No other player (or the same player) can target or risk either of those tiles in another attack this round.

**Visibility:** All players can see that an attack was declared, which tile is targeted, and which tile is at risk.

### Army Selection (Battle Phase Step 1)

After all action phases are complete, all players involved in battles select their armies simultaneously:

1. Both attacker and defender pick armies from their global roster:
   - Up to **3 armies** for standard battles
   - Up to **5 armies** if either the target or risked tile contains a castle
2. Players can commit fewer armies than the max if they don't have enough (even 1 is valid)
3. If a player has **0 available armies**, they enter the battle with nothing and **automatically lose** (combat ends immediately when one side has 0 armies)
4. Each army can only be committed to **one battle per round**
5. Selections are **hidden** during this step
6. **Time limit** applies (configurable). If time expires, armies are auto-selected (strongest first by Attack stat)

### Army Reveal (Battle Phase Step 2)

Both sides' army selections are revealed. Players can see:
- Which army types the opponent selected
- Current HP of each opponent army

### Setting Lineups (Battle Phase Step 3)

After seeing the opponent's armies:

1. Both sides set the **order** of their committed armies (which army fights first, second, third)
2. Lineups are set **simultaneously** — order is hidden from opponent
3. **Time limit** applies (configurable, D-36: 20s base + 5s per battle). If time expires, order defaults to the selection order
4. Once both sides confirm (or time expires), lineups are locked

---

## Combat Resolution

All battles are resolved server-side instantly once lineups are locked. The results are played back to all players afterward.

### Round Flow

**Active army:** Each side has an active army — the current army in their lineup. Starts as the first army in the lineup order.

**Each round:**

**Step 1 — Initiative Roll (Who Strikes)**

The two active armies' Initiative stats are normalized into a probability:

```
attackerChance = attackerArmy.Initiative / (attackerArmy.Initiative + defenderArmy.Initiative)
defenderChance = defenderArmy.Initiative / (attackerArmy.Initiative + defenderArmy.Initiative)
```

A random roll determines which side wins this round.

**Example:** Attacker army has Initiative 80, defender army has Initiative 50.
- Attacker chance: 80/130 = **61.5%**
- Defender chance: 50/130 = **38.5%**

**Step 2 — Damage Roll (Winner Hits Loser)**

The winning side's active army deals damage to the losing side's active army:

```
effectiveAttack = winner.Attack × (winner.currentHP / winner.maxHP)
damage = effectiveAttack × random(winner.DamageRangeMin, winner.DamageRangeMax)
```

Situational bonuses and faction bonuses are applied to Attack before this calculation.

Damage is applied to the **loser's active army**.

**Step 3 — Chip Damage (Loser Hits Back)**

The losing side's active army deals chip damage to the winning side's active army:

```
effectiveAttack = loser.Attack × (loser.currentHP / loser.maxHP)
chipDamage = effectiveAttack × random(loser.ChipDamageRangeMin, loser.ChipDamageRangeMax)
```

Chip damage is applied to the **winner's active army**.

**Step 4 — Cleanup**

- If an army reaches 0 HP, it is **destroyed**
- The next army in that side's lineup becomes the active army
- If a side has no more armies, combat ends — the other side wins
- **Double kill:** If both active armies die in the same round (main damage kills one, chip damage kills the other), the side that won the **initiative roll** that round is the overall winner
- If neither army died, next round begins with the same active armies

### Combat End

Combat ends when one side has no surviving armies.

---

## Battle Outcomes

### Attacker Wins

- Attacker **gains** the defender's targeted tile
- Any building on the captured tile is **destroyed**
- Attacker keeps their risked tile

### Defender Wins

- Defender **gains** the attacker's risked tile
- Any building on the taken tile is **destroyed**
- Defender keeps their targeted tile

### Post-Battle Army State

- Armies that died during combat are **permanently destroyed** (removed from roster)
- Surviving armies return to the player's global roster with their **current HP**
- Army HP heals a small percentage of max HP per round during the Income Phase (global heal rate, admin-editable)

---

## Castle Battles

When either the target tile or the risked tile contains a **castle**:

- Battle size increases from **3v3 to 5v5**
- The castle itself has no separate HP — it is the building on the tile
- If the castle's tile is captured, the building (castle) is **destroyed**
- Losing your castle = **elimination from the game**

---

## Key Design Properties

- **No rock-paper-scissors matchups** — combat is determined by stats and dice
- **All strategy is pre-combat** — army selection and lineup order are the decisions
- **Attrition matters** — HP persists across battles, damaged armies stay damaged
- **HP-to-damage scaling** — wounded armies get progressively weaker
- **Chip damage** — even the winner takes some damage, preventing deathball strategies
- **Risk/reward on tiles** — attacker risks a tile to gain a tile, every battle has stakes for both sides
- **Admin-tunable everything** — all stats, ranges, and bonuses are editable without code changes

---

## Battle Record

Every battle creates a `Battle` record:

| Field | Value |
|-------|-------|
| GameId | Current game |
| RoundNumber | Game round this battle occurred in |
| AttackerKingdomId | Attacking kingdom |
| DefenderKingdomId | Defending kingdom |
| AttackerTileId | Tile the attacker risked |
| DefenderTileId | Tile the attacker targeted |
| Outcome | AttackerWon or DefenderWon |
| TileCapturedId | The tile that changed ownership |
| TileCapturedFromKingdomId | Kingdom that lost the tile |
| OccurredAt | Server timestamp |

Every round within a battle creates a `BattleRound` record:

| Field | Value |
|-------|-------|
| BattleId | Parent battle |
| RoundNumber | Sequential round number within this battle |
| AttackerArmyId | Attacker's active army this round |
| DefenderArmyId | Defender's active army this round |
| InitiativeWinner | Which side won the initiative roll (Attacker/Defender) |
| AttackerInitiativeChance | Calculated % chance for attacker |
| DefenderInitiativeChance | Calculated % chance for defender |
| DamageDealt | Damage applied to the losing army |
| ChipDamageDealt | Chip damage applied to the winning army |
| AttackerArmyHPAfter | Attacker's active army HP after this round |
| DefenderArmyHPAfter | Defender's active army HP after this round |
| ArmyDestroyedId | ID of army destroyed this round (null if none) |

A `TurnLog` entry with `EventType = BattleResolved` is also created, with `Metadata` containing the `battleId`.
