# Systems Reference — Slot Machine (Action Gambling)

> A resource sink that lets players gamble Gold for extra action points during their turn. High risk, high reward tempo mechanic.

---

## Overview

During their action phase, a player can spend Gold to spin a slot machine. The outcome adds or removes action points for the current turn. This gives Gold a unique late-game identity as the "do more stuff" resource — when you're flush with Gold and need tempo, you gamble.

---

## When Can You Spin?

- Any time during your action phase
- Spinning does **not** cost an action point — only Gold
- You can spin **unlimited times** per turn as long as you have Gold
- Results apply immediately — you can spin, gain actions, and use them right away
- Results are **temporary** — gained or lost actions only last for the current turn

---

## Cost Per Spin

**30 Gold** per spin (admin-editable).

The cost is the same every spin — no scaling. This keeps it simple and predictable. The risk comes from the outcome, not the price.

---

## Outcomes

Each spin produces one of 5 outcomes:

| Result | Effect | Suggested Weight |
|--------|--------|-----------------|
| **-2 actions** | Lose 2 action points | ~5% |
| **-1 action** | Lose 1 action point | ~25% |
| **+0 actions** | Nothing happens | ~30% |
| **+1 action** | Gain 1 action point | ~25% |
| **+2 actions** | Gain 2 action points | ~15% |

Expected value is slightly positive (~+0.2 actions per spin) so it's worth doing when you have excess Gold — but the variance means it can backfire.

All weights are **admin-editable**.

---

## Minimum Action Floor

Action points cannot go below **0**. If a bad spin would reduce you below 0, you are set to 0 and your turn effectively ends (no actions remaining).

Example: You have 1 action left, spin and get -2. You go to 0, not -1. Turn is over.

---

## Strategic Role

- **Early game:** Too expensive — Gold is needed for buildings and armies
- **Mid game:** Occasional gamble when you have spare Gold and need one more action
- **Late game:** Primary Gold sink — spin repeatedly to launch multiple attacks in a single turn
- **Merchant Republic synergy:** Cheaper everything means more Gold left over for gambling. Their combat weakness is offset by action advantage from spins

---

## UI Concept

Slot machine / wheel visual in the game HUD. Player clicks to spin, sees the animation, result is applied immediately. Other players can see that a spin happened (via turn log) but don't see the result until it affects actions taken.

---

## Admin Editability

| Field | Type | Notes |
|-------|------|-------|
| SpinCostGold | Integer | Gold cost per spin (on Game entity) |
| SlotOutcomeWeights | JSON string | Weight for each outcome [-2, -1, 0, +1, +2] (on Game entity) |

Action points cannot go below 0 — this is hardcoded, not configurable.

---

## Turn Log

Each spin creates a `TurnLog` entry:

| Field | Value |
|-------|-------|
| EventType | SlotMachineSpin |
| Description | "Kingdom X spun the slot machine: +1 action" |
| Metadata | { goldSpent, outcome, actionsAfter } |
