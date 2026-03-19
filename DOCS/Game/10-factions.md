# Systems Reference — Factions

> Defines all playable factions, their bonuses, weaknesses, and how they interact with game systems. All values are admin-editable.

---

## Overview

Each player picks a faction when joining a lobby. **No two players in the same game can pick the same faction.** With max 4 players, all 4 factions can be in play simultaneously.

Factions apply passive modifiers throughout the game — combat, economy, and action system. Every faction has a clear strength and a genuine weakness that pushes a different playstyle.

---

## Faction Design Principles

- Every faction has a **primary identity** that defines how you play
- Every faction has **strengths across multiple systems** (not just combat or just economy)
- Every faction has a **weakness** that creates a real tradeoff
- Bonuses are in the **10-20% range** — impactful but not game-breaking
- All values are **admin-editable** for balance tuning

---

## Iron Throne — The Warmongers

**Identity:** Win through combat superiority. Your armies hit harder but your economy is weaker.

**Playstyle:** You can't out-economy anyone, so you MUST attack early and often. Your combat edge makes every fight slightly in your favor. You want to take enemy tiles rather than build your own economy.

| Modifier | Value | System |
|----------|-------|--------|
| Army Attack | **+15%** all army types | Combat |
| Chip Damage Ranges | **×1.50** all army types | Combat |
| Resource Production | **-15%** all buildings | Economy |

### How Bonuses Apply

- **Attack bonus:** Applied to base Attack stat of all armies before combat. Stacks with situational bonuses (attacker/defender).
- **Chip Damage bonus:** Applied as a multiplier on both min and max of Chip Damage Range (e.g. 0-20% becomes 0-30% at 1.50x). Note: a 0% min stays 0% since 0 × anything = 0.
- **Resource penalty:** Applied as a multiplier on all building yields during Income Phase (yield × 0.85).

### Starting Bonus
+50 Gold (compensates for weaker early economy)

---

## Mage Council — The Strategists

**Identity:** Win through initiative control and action advantage. Your armies strike first but are fragile.

**Playstyle:** You get more actions so you can build AND attack in the same turn when others can't. Your high initiative means your lineup order is devastating — your first army almost always strikes. But your armies are glass — if you lose initiative rolls, you get punished hard.

| Modifier | Value | System |
|----------|-------|--------|
| Army Initiative | **+20%** all army types | Combat |
| Action Points | **+1** per turn | Action System |
| Army HP | **-15%** all army types | Combat |

### How Bonuses Apply

- **Initiative bonus:** Applied to base Initiative stat of all armies before combat. Higher initiative = better odds of winning each round roll.
- **Action Point bonus:** Added to the base action point count at the start of each turn.
- **HP penalty:** Applied to max HP of all armies (maxHP × 0.85). Current HP scales proportionally. Affects damage output via HP-to-damage scaling.

### Starting Bonus
+30 Mana (enables early Mage training if Mana economy is built)

---

## Merchant Republic — The Economists

**Identity:** Win through economic snowball. Cheaper everything, but weaker in direct combat.

**Playstyle:** You expand faster and recover faster from losses because everything is cheaper. You can field more armies than anyone else. But each individual army is weaker, so you need to overwhelm with numbers. Losing a battle hurts less because retraining is cheap.

| Modifier | Value | System |
|----------|-------|--------|
| Building Costs | **-20%** all buildings | Economy |
| Army Training Costs | **-15%** all army types | Economy |
| Army Attack | **-10%** all army types | Combat |

### How Bonuses Apply

- **Building cost reduction:** Applied as a multiplier on all building costs (cost × 0.80). Rounded up (ceil).
- **Training cost reduction:** Applied as a multiplier on all army training costs (cost × 0.85). Rounded up (ceil).
- **Attack penalty:** Applied to base Attack stat of all armies before combat. Stacks with situational bonuses.

### Starting Bonus
+100 Gold (enables rapid early expansion)

---

## Forest Elves — The Survivors

**Identity:** Win through attrition and outlasting opponents. Your armies are tough and heal fast, but you're slow.

**Playstyle:** Your armies survive fights that would kill others, and they bounce back faster. But you have fewer actions, so every decision matters more. You want long games where attrition wears everyone else down. You're the hardest faction to rush but the slowest to expand.

| Modifier | Value | System |
|----------|-------|--------|
| Army HP | **+15%** all army types | Combat |
| Heal Rate | **+50%** (e.g. 10% base → 15%) | Army Recovery |
| Action Points | **-1** per turn | Action System |

### How Bonuses Apply

- **HP bonus:** Applied to max HP of all armies (maxHP × 1.15). Current HP scales proportionally. Also increases effective damage via HP-to-damage scaling.
- **Heal rate bonus:** Applied as a multiplier on the global heal percentage. If base heal is 10%, Elves heal at 15% per round.
- **Action Point penalty:** Subtracted from the base action point count at the start of each turn.

### Starting Bonus
+50 Wood (enables early building)

---

## Faction Matchup Dynamics

| Matchup | Favored | Why |
|---------|---------|-----|
| Iron Throne vs Merchant Republic | Iron Throne | Combat bonuses crush weaker armies, even in large numbers |
| Iron Throne vs Forest Elves | Forest Elves | High HP absorbs Iron Throne's damage, heal rate outlasts aggression |
| Iron Throne vs Mage Council | Depends | Initiative vs raw power — comes down to lineup choices |
| Mage Council vs Forest Elves | Forest Elves | Fragile Mage Council armies can't wear down Elf HP pools |
| Mage Council vs Merchant Republic | Mage Council | Extra action + initiative control outmaneuvers cheap armies |
| Merchant Republic vs Forest Elves | Merchant Republic | Numbers overwhelm despite Elf durability; cheap retraining beats attrition |

These are soft matchups, not hard counters. Player skill and game state matter more than faction choice.

---

## Faction Bonus Application Order

When multiple bonuses apply to the same stat:

1. Start with base stat (from army type definition)
2. Apply faction modifier
3. Apply situational bonus (attacker/defender)
4. Result is the effective stat used in combat

For resource yields:
1. Start with BuildingType.BaseYield
2. Apply terrain multiplier
3. Apply faction resource modifier
4. Result is the yield added to kingdom resources

All multipliers are **multiplicative** (stacked), not additive.

---

## Admin Editability

All faction values are stored in the database and editable via the admin panel:

| Field | Type | Notes |
|-------|------|-------|
| Name | String | Display name |
| Description | String | Flavour text |
| AttackModifier | Decimal | Multiplier on army Attack (e.g. 1.15, 0.90) |
| HPModifier | Decimal | Multiplier on army HP (e.g. 1.15, 0.85) |
| InitiativeModifier | Decimal | Multiplier on army Initiative (e.g. 1.20) |
| ChipDamageModifier | Decimal | Multiplier on chip damage ranges (e.g. 1.10) |
| ResourceProductionModifier | Decimal | Multiplier on building yields (e.g. 0.85) |
| BuildingCostModifier | Decimal | Multiplier on building costs (e.g. 0.80) |
| TrainingCostModifier | Decimal | Multiplier on army training costs (e.g. 0.85) |
| ActionPointModifier | Integer | Added to base action points (e.g. +1, -1) |
| HealRateModifier | Decimal | Multiplier on global heal rate (e.g. 1.50) |
| StartingBonusResource | Enum | Which resource gets the starting bonus |
| StartingBonusAmount | Integer | How much extra of that resource |
