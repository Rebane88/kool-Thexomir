# Systems Reference — Army Types

> Defines all army types, their stats, unlock requirements, costs, and situational bonuses. All values are admin-editable and will be balanced through playtesting.

---

## Overview

Armies are single entities trained from military buildings and added to the player's global roster. Each army type has a distinct risk profile that creates different tradeoffs when chosen during combat rounds.

There are no rock-paper-scissors matchups. Combat outcomes are determined by stats, dice rolls, and player decisions (lineup order).

---

## Army Stats

Every army type has these stats (all admin-editable):

| Stat | Purpose |
|------|---------|
| Attack | Base damage dealt when winning a round |
| HP | Health pool — destroyed at 0 |
| Initiative | Chance weight for winning the round roll |
| Damage Range (min–max %) | Percentage range of effective Attack dealt on hit |
| Chip Damage Range (min–max %) | Percentage range of loser's effective Attack dealt back |
| Training Cost | One-time resource cost to train |
| Upkeep Cost | Per-round resource cost to maintain |
| Required Building | Military building needed to train this type |

---

## Army Types

### Warrior
**Identity:** Balanced and reliable. No major strength, no major weakness. The backbone of any army.

| Stat | Value |
|------|-------|
| Attack | 25 |
| HP | 100 |
| Initiative | 50 |
| Damage Range | 60–100% |
| Chip Damage Range | 0–15% |
| Situational Bonus | +10% Attack when defending |
| Required Building | Barracks (Tier 1) |

---

### Scout
**Identity:** Fast and evasive. Wins initiative often but hits light and dies fast.

| Stat | Value |
|------|-------|
| Attack | 15 |
| HP | 60 |
| Initiative | 70 |
| Damage Range | 50–80% |
| Chip Damage Range | 0–10% |
| Situational Bonus | None |
| Required Building | Barracks (Tier 1) |

---

### Knight
**Identity:** Premium heavy unit. Strong and sturdy but slow to act. Expensive.

| Stat | Value |
|------|-------|
| Attack | 35 |
| HP | 140 |
| Initiative | 30 |
| Damage Range | 60–100% |
| Chip Damage Range | 0–15% |
| Situational Bonus | +10% HP when defending |
| Required Building | Stables (Tier 2) |

---

### Berserker
**Identity:** High risk, high reward. Devastating on offense, fragile if caught.

| Stat | Value |
|------|-------|
| Attack | 35 |
| HP | 60 |
| Initiative | 50 |
| Damage Range | 70–100% |
| Chip Damage Range | 0–10% |
| Situational Bonus | +15% Attack when attacking |
| Required Building | Stables (Tier 2) |

---

### Mage
**Identity:** Glass cannon with initiative. Often strikes first and hits hard, but wildly inconsistent and extremely fragile.

| Stat | Value |
|------|-------|
| Attack | 35 |
| HP | 60 |
| Initiative | 70 |
| Damage Range | 40–100% |
| Chip Damage Range | 0–5% |
| Situational Bonus | +20% Attack when attacking |
| Required Building | War Academy (Tier 3) |

---

### Guardian
**Identity:** The wall. Absorbs hits, punishes attackers with high chip damage. Rarely strikes first but very hard to kill.

| Stat | Value |
|------|-------|
| Attack | 15 |
| HP | 140 |
| Initiative | 30 |
| Damage Range | 50–90% |
| Chip Damage Range | 5–25% |
| Situational Bonus | +15% Initiative when defending |
| Required Building | War Academy (Tier 3) |

---

## Military Building Unlock Progression

| Building | Tier | Unlocks | Game Phase |
|----------|------|---------|------------|
| Barracks | 1 | Warrior, Scout | Early game |
| Stables | 2 | Knight, Berserker | Mid game |
| War Academy | 3 | Mage, Guardian | Late game |

Higher tier buildings are upgrades on the same tile (Barracks → Stables → War Academy). Building Stables replaces Barracks but still allows training of Warrior and Scout (all lower tier unlocks are retained).

Each military building supports up to **3 armies** at a time. To train more, build additional military buildings. When an army dies in combat, its slot is freed.

---

## Training Costs

Costs scale with army power. Resource types vary by army type. All admin-editable.

| Type | Gold | Food | Stone | Mana | Notes |
|------|------|------|-------|------|-------|
| Warrior | 40 | 10 | — | — | Cheap, accessible |
| Scout | 25 | — | — | — | Cheapest unit |
| Knight | 80 | 20 | 15 | — | Expensive, premium |
| Berserker | 60 | 15 | — | — | Moderate |
| Mage | 70 | — | — | 30 | Requires Mana economy |
| Guardian | 60 | 10 | 20 | — | Requires Stone economy |

---

## Upkeep Costs

Every army costs resources per round to maintain. All admin-editable (can be set to 0).

| Type | Gold | Food | Mana |
|------|------|------|------|
| Warrior | 3 | 2 | — |
| Scout | 2 | 1 | — |
| Knight | 6 | 4 | — |
| Berserker | 4 | 3 | — |
| Mage | 5 | — | 2 |
| Guardian | 4 | 3 | — |

If a kingdom cannot afford upkeep after income, armies are disbanded starting from the most expensive upkeep first.

---

## Situational Bonuses Summary

| Type | Bonus | Condition |
|------|-------|-----------|
| Warrior | +10% Attack | When defending |
| Scout | None | — |
| Knight | +10% HP | When defending |
| Berserker | +15% Attack | When attacking |
| Mage | +20% Attack | When attacking |
| Guardian | +15% Initiative | When defending |

Bonuses are determined by whether the player is the **attacker** (declared the attack) or **defender** (being attacked) in the battle. Applied before combat resolution.

---

## Faction Bonuses on Armies

Factions apply passive modifiers to army stats. Examples (to be rebalanced):

- **Iron Throne:** +X% Attack on all armies
- **Mage Council:** +X% Attack on Mage armies
- **Merchant Republic:** -X% Attack on all armies (weakness)
- **Forest Elves:** No army modifier

Exact faction army bonuses TBD — will be balanced alongside army stats.

---

## Key Design Properties

- **No matchups** — no type hard-counters another. Every fight is winnable with any army
- **Distinct risk profiles** — each type represents a different gamble in the round-by-round combat
- **Progression gated** — stronger types require higher tier buildings, creating natural power curves
- **Economic pressure** — powerful armies cost more to train and maintain
- **Admin-tunable** — all stats, costs, and bonuses are editable without code changes
