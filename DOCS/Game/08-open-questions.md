# Open Questions & Decision Log

> Every unresolved design question lives here. When you make a decision, move it to the Decision Log at the bottom with a reason. This file is your memory.

---

## ⚠️ OPEN QUESTIONS

Questions are grouped by system. Answer them before implementing that system.

---

### Turn Structure

**OQ-01** — Turn time limit expiry
What happens when `TurnDeadline` is reached and the player hasn't ended their turn?
- Does the server auto-end it?
- If yes, what server mechanism handles this — a background job (Hangfire, hosted service), or checked on the next incoming request?

**OQ-02** — Action limit per turn
Is there a cap on how many actions a player can take per turn?
- Option A: Unlimited (simpler, but turns can take forever in large kingdoms)
- Option B: Fixed action points (e.g. 5 per turn) — forces meaningful choices
- Option C: Action limit scales with kingdom size

**OQ-03** — Multiple attacks per turn
Can a player attack more than one enemy tile in a single turn?

**OQ-04** — Move + attack in same turn
Can the same army both move (onto an owned tile) AND attack in the same turn, or does moving spend its attack?

**OQ-05** — Retreat with no valid destination
If the defending army loses and has no adjacent owned tile to retreat to — what happens?
- Option A: Remaining units are destroyed
- Option B: Units are captured (transfer to attacker)
- Option C: Units retreat to the nearest owned tile (not just adjacent)

**OQ-06** — Attacker retreat with no origin tile
If the attacker loses, they retreat to the tile they attacked from. But what if that tile was captured by someone else in the same turn?

---

### Combat

**OQ-07** — Mixed army matchup calculation
When a mixed army fights a mixed defending army, how is the matchup modifier calculated per unit?
- Option A: Each attacker unit matched against most common defender unit type
- Option B: Each attacker unit matched against average matchup across all defender types
- Option C: Overall matchup is average of all attacker-defender pairs

**OQ-08** — Attacker terrain
Does the terrain the attacker is attacking *from* have any effect? Currently: no.

**OQ-09** — Multiplication order
Exact order of modifiers in combat formula: matchup → terrain → faction → dice? Define and fix.

**OQ-10** — Exact tie outcome
If attacker and defender final power are identical after dice rolls — who wins?

**OQ-11** — Casualty formula
Exact formula for calculating units lost:
- Option A: Loser loses % of units proportional to power difference
- Option B: Loser always loses all units
- Option C: Both sides take casualties proportional to opposing power

**OQ-12** — Winner casualties
Does the winning side take any casualties at all?

**OQ-13** — Post-win army merge on captured tile
If attacker wins and moves their army to the captured tile, and they already had an army there — do the armies merge?

---

### Resources

**OQ-14** — Base starting resources
What are the base starting resources for all kingdoms (before faction bonus)?
Suggested: Gold 100, Food 50, Wood 30, Stone 10, Mana 0

**OQ-15** — Terrain + faction multiplier stacking
Are terrain and faction resource multipliers applied multiplicatively or additively?
Example: Forest Elves on Forest tile for Wood — 1.2 × 1.2 = 1.44x, or 1.2 + 0.2 = 1.4x?

**OQ-16** — Tiles without buildings
Do tiles without buildings generate any base resources? Current assumption: no.

**OQ-17** — Tile claim cost
What is the Gold cost to claim an unclaimed tile?

**OQ-18** — Building cost rounding
When applying faction BuildingCostModifier, round up or round down?

**OQ-19** — Resource storage cap
Should there be a maximum storage cap for resources? A cap would prevent passive hoarding in late game.
- Option A: No cap
- Option B: Cap exists, increased by certain buildings (e.g. Granary raises Food cap, Bank raises Gold cap)

**OQ-20** — Upkeep sort order for disbanding
When disbanding units due to upkeep failure, sort by "most expensive" — define this:
- Option A: Sort by UpkeepGold descending
- Option B: Sort by UpkeepFood descending
- Option C: Sort by combined upkeep value descending

---

### Buildings

**OQ-21** — Building upgrade replaces or stacks
When building Tier 2 on a tile, is the Tier 1 building replaced (one building per tile) or does Tier 2 exist alongside it?
Current assumption: replaced.

**OQ-22** — Terrain-restricted buildings
Should any buildings be restricted to specific terrain? Candidates: Lumber Camp → Forest only, Quarry → Mountain only.

**OQ-23** — Building.KingdomId on capture
When a tile is captured, should `Building.KingdomId` update to the new owner? Or is it purely historical and tile ownership is the authority?

**OQ-24** — Defense building bonus
How do Palisade/Stone Wall/Fortress buildings affect combat defense?
- Option A: Add a flat bonus on top of terrain defense (e.g. Fortress +20%)
- Option B: Multiplier on terrain bonus
- Option C: Separate mechanic (e.g. attacker takes casualties even when winning)
This must be decided before combat is implemented — it affects the Catapult's "strong against fortified tiles" ability.

---

### Win Conditions

**OQ-25** — Domination default threshold
Recommended DominationThreshold default to show in lobby UI?

**OQ-26** — Domination tile count
Does DominationThreshold count barbarian-owned tiles in the denominator?

**OQ-27** — Simultaneous elimination tie
If the last two active kingdoms are eliminated simultaneously, who wins?

**OQ-28** — Score formula
Define the exact score formula for Score win condition.

**OQ-29** — Score visibility
Is the score leaderboard visible to all players in real time?

**OQ-30** — Score tiebreak
Define tiebreak rules if two kingdoms have identical scores at MaxTurns.

**OQ-31** — Defeated kingdom army cleanup
When a kingdom is defeated, are their remaining armies/units deleted, or do they remain as neutral entities?

---

### Map Generation

**OQ-32** — Recommended map sizes per player count

**OQ-33** — Hex adjacency formula
Confirm the exact axial coordinate adjacency formula used consistently everywhere.

**OQ-34** — Terrain distribution weights
Define percentage weights for each terrain type.

**OQ-35** — Terrain clustering
Purely random per tile, or noise-based clustering for regions?

**OQ-36** — Player spawn algorithm
Exact spawn placement for 2, 3, 4, 5, 6, 7, 8 player games.

**OQ-37** — Starting tile terrain
Is the starting tile always Plains or random?

**OQ-38** — Starting army
Does each player start with a pre-built army (e.g. 2 Swordsmen)?

**OQ-39** — Starting building
Does the starting tile have a pre-built building?

**OQ-40** — Barbarian army size
How many Swordsmen per barbarian camp?

**OQ-41** — Barbarian scaling
Should barbarian army size scale with distance from player starts?

---

---

## ✅ DECISION LOG

*When you decide an open question, move it here with your answer and reason.*

| ID | Question Summary | Decision | Reason | Date |
|----|-----------------|----------|--------|------|
| — | — | — | — | — |

---

## 💡 DEFERRED FEATURES

Features explicitly scoped out of v1 — do not design or implement until noted:

- Diplomacy (trade, alliances, non-aggression pacts)
- Random events (GameEvent) — entity exists but firing logic not implemented
- Fog of war
- Observer mode for defeated players
- Replay system
- Resource storage caps (OQ-19 — decide in v1 whether to include)
