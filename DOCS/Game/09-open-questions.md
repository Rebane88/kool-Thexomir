# Open Questions & Decision Log

> Every unresolved design question lives here. When you make a decision, move it to the Decision Log at the bottom with a reason. This file is your memory.

---

## ⚠️ OPEN QUESTIONS

---

### Action System

*OQ-01 and OQ-02 resolved — see Decision Log D-29, D-30*

*OQ-03 resolved — see Decision Log D-35*

---

### Combat

*OQ-04 resolved — see Decision Log D-36*

*OQ-05/06 resolved — covered by D-32 (tile locking). Both tiles in a battle are locked, preventing double-booking. Mutual attacks on different tiles are allowed — both battles resolve.*

*OQ-07 resolved — see Decision Log D-31*

---

### Army Healing / Repair

*Resolved — see Decision Log D-25*

---

### Economy

*OQ-09 resolved — see Decision Log D-37*

*OQ-10, OQ-11, OQ-12, OQ-13 resolved — see Decision Log D-34*

---

### Win Conditions

*OQ-14 through OQ-17 no longer applicable — Domination and Score modes removed. Elimination only (D-26). Castle loss = elimination (D-16).*

---

### Map

*OQ-18 resolved — see Decision Log D-33*

*OQ-19 resolved — see Decision Log D-38*

---

### Factions

*Resolved — see Decision Log D-28*

---

## ✅ DECISION LOG

| ID | Question Summary | Decision | Reason | Date |
|----|-----------------|----------|--------|------|
| D-01 | Action limit per turn | Action point system — each action costs points | Forces meaningful choices, prevents long turns | 2026-03-18 |
| D-02 | Turn time limits | Yes, configurable per lobby | Prevents stalling in multiplayer | 2026-03-18 |
| D-03 | Tile claiming mechanic | Building a tier 1 building claims adjacent unowned tiles | Ties expansion to economy, no gold-based claiming | 2026-03-18 |
| D-04 | Army positioning | Global roster — armies are not on the map | Simplifies movement, combat uses pool system | 2026-03-18 |
| D-05 | Combat system | Round-by-round gambling with pre-set lineups | Interactive, exciting, admin-tunable | 2026-03-18 |
| D-06 | Battle stakes | Attacker risks a tile, winner takes loser's tile | Both sides have skin in the game | 2026-03-18 |
| D-07 | Battle size | 3v3 standard, 5v5 if castle involved | Castle battles feel like proper sieges | 2026-03-18 |
| D-08 | Combat timing | Battles declared during action phase, resolved after all players finish | No idle waiting, all strategy is upfront | 2026-03-18 |
| D-09 | Lineup selection | Players set army order before combat, hidden from opponent | Mind-game layer, no mid-combat decisions | 2026-03-18 |
| D-10 | Army survival | Armies that survive combat return to roster with current HP | Natural attrition, not winner-takes-all | 2026-03-18 |
| D-11 | HP-to-damage scaling | Linear — effectiveAttack = Attack × (currentHP/maxHP) | Simple, easy to balance | 2026-03-18 |
| D-12 | Initiative degradation | Initiative does NOT degrade with HP | Wounded armies being slower would be too punishing | 2026-03-18 |
| D-13 | Barbarian system | Removed entirely | Not needed — expansion is building-based, no obstacles | 2026-03-18 |
| D-14 | Defense buildings | Removed entirely (Palisade, Stone Wall, Fortress) | Combat redesign made them irrelevant | 2026-03-18 |
| D-15 | Building on captured tile | Building is always destroyed on capture | Prevents snowballing from stolen infrastructure | 2026-03-18 |
| D-16 | Castle loss | Losing castle = elimination | Clear win objective, high stakes | 2026-03-18 |
| D-17 | Army types | 6 types with distinct risk profiles, no matchups | Warrior, Scout, Knight, Berserker, Mage, Guardian | 2026-03-18 |
| D-18 | Double kill tiebreaker | Side that won initiative roll that round wins | Clean, uses existing mechanic | 2026-03-18 |
| D-19 | Terrain bonuses | Terrain gives yield bonus to buildings only, not armies | Clean separation — armies have their own situational bonuses | 2026-03-18 |
| D-20 | Army situational bonuses | Based on attacker/defender role, not tile position | Armies are global so tile-based bonuses don't apply | 2026-03-18 |
| D-21 | Resource multiplier stacking | Multiplicative (terrain × faction) | More interesting specialisation | 2026-03-18 |
| D-22 | Building upgrade | Replaces previous tier (one building per tile) | Simpler to manage and understand | 2026-03-18 |
| D-23 | Tiles without buildings | Generate no resources | Buildings are the economic engine | 2026-03-18 |
| D-24 | Cost rounding | Ceil (round up) | Prevents fractional discount exploits | 2026-03-18 |
| D-25 | Army healing | Passive % heal per round during Income Phase | Simple, no special mechanics, admin-tunable rate (5-10%) | 2026-03-18 |
| D-26 | Win conditions | Elimination only — lose castle = eliminated | Simpler, more decisive games | 2026-03-18 |
| D-27 | Max players | 4 players max (2v2 team mode deferred) | Keep it simple | 2026-03-18 |
| D-28 | Factions | 4 factions redesigned: Iron Throne (combat), Mage Council (initiative+actions), Merchant Republic (economy), Forest Elves (attrition) | Each faction pushes a different playstyle with clear strength and weakness | 2026-03-18 |
| D-29 | Base action points | 4 per turn (Mage Council 5, Forest Elves 3) | Balanced starting point, factions modify it | 2026-03-18 |
| D-30 | Slot machine | Gold-sink gambling for actions: spend Gold, win/lose -2 to +2 actions, unlimited spins per turn | Late-game tempo mechanic, gives Gold unique identity | 2026-03-18 |
| D-31 | Army roster limit | 3 armies per military building (D-50 supersedes). Economy + building capacity both gate army count | Military buildings are high-value targets | 2026-03-18 |
| D-32 | Tiles locked during battle | Both tiles involved in a battle (target tile AND risked tile) are locked for the round — no other attack can target or risk either of them | Prevents conflicts where multiple attacks involve the same tiles | 2026-03-18 |
| D-33 | Map sizes | 2p: 16x16 (256), 3p: 20x20 (400), 4p: 24x24 (576) | Slightly bigger maps, scale with player count (max 4) | 2026-03-18 |
| D-34 | Balance values | Concrete stats for army types (Atk 15/25/35, HP 60/100/140, Init 30/50/70), building costs (Tier 1-3), training costs, upkeep costs, 30 Gold spin cost | Replace all TBD/Low/Med/High placeholders with real numbers | 2026-03-18 |
| D-35 | Turn timer | 2 minutes per turn (configurable per lobby) | Enough time to think without stalling | 2026-03-18 |
| D-36 | Lineup timer | 20 seconds base + 5 seconds per battle the player is in. Use the max value across all players | More battles = more time needed to set lineups | 2026-03-18 |
| D-37 | Starting resources | Gold 150, Food 60, Wood 50, Stone 20, Mana 0 | Enough for 2 tier-1 buildings + 1 army on first turn, will tune in balancing | 2026-03-18 |
| D-38 | Terrain distribution | Anti-clustering (max 2 same-type neighbors) + starting area variety (4 of 5 types within 3 hexes of spawn) | Players need meaningful expansion choices, no large same-terrain blobs | 2026-03-18 |
| D-39 | River → Desert | Replaced River terrain with Desert for Gold bonus | River didn't fit thematically, Desert feels right for trade/wealth | 2026-03-18 |
| D-40 | Army selection timing | Armies selected in Battle Phase (not Action Phase). Both sides pick simultaneously, then revealed, then set lineup order | Defender needs to see attack before committing armies, creates mind-game layer | 2026-03-18 |
| D-41 | Terrain yield multiplier | 1.10x (10%) for all matching terrain, admin-editable per terrain | Simple starting point, tune during testing | 2026-03-18 |
| D-42 | Castle yield | Castle produces +10 Food, +10 Wood, +10 Stone per round | Baseline income for every kingdom | 2026-03-18 |
| D-43 | Army count per battle | Up to 3 (or 5 for castle), not exactly 3. Can fight with fewer. 0 armies = auto-lose | Don't block gameplay when short on armies | 2026-03-18 |
| D-44 | Risking castle tile | Allowed — player can risk elimination on an offensive attack | May be the only tile left, why block it | 2026-03-18 |
| D-45 | Defeated kingdom cleanup | All tiles become unowned, all buildings destroyed, all armies deleted | Clean slate — other players expand into freed territory via building | 2026-03-18 |
| D-46 | Simultaneous castle destruction | Both kingdoms eliminated. If last two, game ends in draw (no winner) | Fair — no arbitrary tiebreaker | 2026-03-18 |
| D-47 | Max round limit | 100 rounds — game ends in draw if reached (admin-editable) | Safety valve against infinite stalemates | 2026-03-18 |
| D-48 | Slot machine results duration | Gained/lost actions last current turn only | Prevents permanent snowball from lucky spins | 2026-03-18 |
| D-49 | Action point floor | 0 — no minimum. Slot machine can bring you to 0 | It's a gamble, that's the risk | 2026-03-18 |
| D-50 | Military building capacity | 3 armies per building. Destroying the building destroys its tied armies | Limits army spam, makes military buildings high-value targets | 2026-03-18 |
| D-51 | Attack requires armies | Must have at least 1 army to declare an attack | Prevents pointless attacks that auto-lose | 2026-03-18 |
| D-52 | Player spawn positions | Edge of map or within 2-3 tiles of edge. 2p: opposite sides, 3p: triangle, 4p: corners | Players expand inward, fair starting distance | 2026-03-18 |
| D-53 | Faction modifiers all multiplicative | All faction modifiers use multipliers, including chip damage (×1.50 for Iron Throne) | Consistent system, no additive exceptions | 2026-03-18 |

---

## 💡 DEFERRED FEATURES

Features explicitly scoped out — do not design or implement until noted:

- Diplomacy (trade, alliances, non-aggression pacts)
- Random events (GameEvent) — entity exists but not designed
- Fog of war
- Observer mode for defeated players
- Replay system
- Resource storage caps
