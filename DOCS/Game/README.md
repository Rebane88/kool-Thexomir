# Realms of Ash — Design Documentation

## Document Index

| File | Purpose | Audience |
|------|---------|----------|
| `01-player-rulebook.md` | How to play — plain language rules | Players, UI design reference |
| `02-turn-structure.md` | Exact turn phase sequence and ordering | Backend implementation |
| `03-combat-system.md` | Combat formula, casualties, outcomes | Backend implementation |
| `04-resource-system.md` | Resource generation, spending, upkeep | Backend implementation |
| `05-building-system.md` | Building placement, unlock chains, defense | Backend implementation |
| `06-win-conditions.md` | Victory check logic for all 3 modes | Backend implementation |
| `07-map-generation.md` | Map and starting state generation | Backend implementation |
| `08-open-questions.md` | All unresolved design decisions + log | You — work through this |

---

## How to Use These Docs

**Starting a new system?** Open the relevant systems reference doc first. Every `⚠️ OPEN QUESTION` in that doc needs an answer before you code.

**Made a decision?** Move the question from `08-open-questions.md` into the Decision Log table with your answer and reason. Then update the relevant system doc to reflect the decision.

**Something contradicts something else?** The systems reference docs are the authority. The player rulebook is derived from them.

---

## Current Status

| System | Design Status | Implementation Status |
|--------|--------------|----------------------|
| Turn Structure | Mostly defined, ~6 open questions | Basic loop working |
| Combat | Core formula defined, casualty formula TBD | Basic working |
| Resources | Generation formula defined, starting values TBD | Basic working |
| Buildings | Mostly defined, defense building bonus TBD | Basic working |
| Win Conditions | All 3 modes defined, formulas TBD | TBD |
| Map Generation | Structure defined, all numbers TBD | Basic working |
| Random Events | Deferred | Not started |

---

## Priority Open Questions

Work through `08-open-questions.md` in this order — these block the most implementation work:

1. **OQ-07** — Mixed army matchup calculation (blocks combat)
2. **OQ-11 / OQ-12** — Casualty formula (blocks combat)
3. **OQ-24** — Defense building bonus (blocks Fortress + Catapult)
4. **OQ-02** — Action limit per turn (affects entire game feel)
5. **OQ-14** — Starting resources (needed before any playtesting)
6. **OQ-17** — Tile claim cost (needed before any playtesting)
7. **OQ-38 / OQ-39** — Starting army and building (affects early game)
