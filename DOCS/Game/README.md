# Realms of Ash — Design Documentation

## Document Index

| File | Purpose | Audience |
|------|---------|----------|
| `01-player-rulebook.md` | How to play — plain language rules | Players, UI design reference |
| `02-turn-structure.md` | Round phases: action → battle → income → end | Backend implementation |
| `03-combat-system.md` | Round-by-round gambling combat with lineups | Backend implementation |
| `04-resource-system.md` | Resource generation, spending, upkeep | Backend implementation |
| `05-building-system.md` | Building placement, upgrades, territory expansion, military capacity | Backend implementation |
| `06-win-conditions.md` | Victory check logic — elimination only | Backend implementation |
| `07-map-generation.md` | Map and starting state generation | Backend implementation |
| `08-army-types.md` | Army type definitions, stats, unlock progression | Backend implementation, balance reference |
| `09-open-questions.md` | Decision log (all questions resolved) | Reference |
| `10-factions.md` | Faction definitions, bonuses, weaknesses, matchup dynamics | Backend implementation, balance reference |
| `11-slot-machine.md` | Action gambling mechanic — spend Gold, gain/lose actions | Backend implementation |

---

## How to Use These Docs

**Starting a new system?** Open the relevant systems reference doc first.

**Made a decision?** Add it to the Decision Log in `09-open-questions.md` with your answer and reason. Then update the relevant system doc to reflect the decision.

**Something contradicts something else?** The systems reference docs are the authority. The player rulebook is derived from them.

---

## Current Status

| System | Design Status | Notes |
|--------|--------------|-------|
| Turn Structure | **Defined** | Round-based: action → battle → income → end |
| Combat | **Defined** | Round-by-round gambling, army selection + lineups in Battle Phase, up to 3v3/5v5 |
| Resources | **Defined** | 5 resources, all values set (admin-editable) |
| Buildings | **Defined** | 6 chains (5 resource + 1 military), 3 army capacity per military building |
| Army Types | **Defined** | 6 types with distinct risk profiles, all stats set |
| Win Conditions | **Defined** | Elimination only — lose castle = eliminated, 100 round max |
| Slot Machine | **Defined** | Gold-sink gambling for action points, results last current turn only |
| Map Generation | **Defined** | Terrain anti-clustering, edge spawns, fair starting positions |
| Factions | **Defined** | 4 factions with distinct playstyles, all modifiers multiplicative |

---

## Removed Systems

The following systems from the original design have been removed:

- **Barbarian camps** — no longer needed, expansion is building-based
- **Defense building chain** (Palisade, Stone Wall, Fortress) — combat redesign made them irrelevant
- **Rock-paper-scissors matchups** — replaced by stat-based combat with initiative/damage rolls
- **Mixed army squads** — armies are now single entities, not groups of unit types
- **Tile-based army movement** — armies are global, not positioned on the map
- **Gold-based tile claiming** — territory expands automatically when buildings are placed
- **Domination / Score win conditions** — elimination only
