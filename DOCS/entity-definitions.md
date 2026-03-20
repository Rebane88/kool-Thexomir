# Realms of Ash — Entity Definitions

## Overview

14 entities total. Identity tables (AppUser extends IdentityUser), lookup/reference tables (TerrainType, BuildingType, ArmyType, FactionType), and game state tables (Game, Kingdom, Tile, Building, Army, Battle, BattleRound, KingdomResource, TurnLog).

---

## Roles

Two roles provided by ASP.NET Core Identity:

| Role | Assigned | Capabilities |
|------|----------|-------------|
| `Admin` | Manually by server owner | Full CRUD on all reference data via admin panel. Manage users and role assignments. |
| `Player` | Automatically on registration | Create and join games, play. No access to admin panel. |

All reference table CRUD controllers (BuildingType, ArmyType, TerrainType, FactionType) are secured with `[Authorize(Roles = "Admin")]`. This gives admins live control over game balance, faction modifiers, and icon URLs without code changes.

---

## 1. AppUser

Extends ASP.NET Core `IdentityUser`. Represents a registered player account. All users are assigned the `Player` role on registration. `Admin` role is assigned manually.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK, inherited | From IdentityUser |
| UserName | string | inherited | From IdentityUser |
| Email | string | inherited | From IdentityUser |
| PasswordHash | string | inherited | From IdentityUser |
| DisplayName | string | required | Shown in game UI |
| AvatarUrl | string | nullable | Profile picture |
| CreatedAt | DateTime | required | Registration timestamp |

**Relationships:**
- One AppUser → many Kingdom (one per game, but only one active game at a time)

**Business Rules:**
- A user can only be in one active game at a time
- Enforced via API check on game join — user must have no Kingdom in a game with Status = Active
- Default role = `Player` on registration
- `Admin` role assigned manually via admin panel

---

## 2. Game

Represents a match instance. Covers both the lobby phase and the active game.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| Name | string | required | Lobby name e.g. "Epic Battle #1" |
| Status | enum | required | Lobby, Active, Finished |
| MaxPlayers | int | required | 2–4 |
| RoundNumber | int | required, default 0 | Current round |
| MaxRounds | int | required, default 100 | Game ends in draw if reached |
| CurrentPhase | enum | required | Action, Battle, Income, RoundEnd |
| CurrentTurnKingdomId | Guid | nullable, FK → Kingdom | Whose turn it is (Action Phase only) |
| MapWidth | int | required | Hex grid width |
| MapHeight | int | required | Hex grid height |
| TurnTimeLimit | int | nullable | Seconds per turn, null = unlimited |
| TurnDeadline | DateTime | nullable | When current turn expires |
| BaseActionPoints | int | required, default 4 | Action points per turn (admin-editable) |
| HealPercent | decimal | required, default 0.10 | Army heal rate per round (5-10%) |
| SpinCostGold | int | required, default 30 | Slot machine cost per spin |
| SlotOutcomeWeights | string | required | JSON array of weights for outcomes [-2,-1,0,+1,+2] e.g. "[5,25,30,25,15]" |
| CreatedByUserId | Guid | required, FK → AppUser | Game creator |
| WinnerKingdomId | Guid | nullable, FK → Kingdom | Set when game finishes, null = draw |
| CreatedAt | DateTime | required | |
| StartedAt | DateTime | nullable | When Status → Active |
| FinishedAt | DateTime | nullable | When Status → Finished |

**Relationships:**
- One Game → many Kingdom
- One Game → many Tile
- One Game → many TurnLog
- One Game → many Battle

**Business Rules:**
- TurnDeadline = now + TurnTimeLimit each time a new turn begins (if TurnTimeLimit is set)
- Game moves to Active when creator starts it (minimum 2 players must have joined)
- Game ends in draw if RoundNumber reaches MaxRounds
- Win condition is always Elimination (lose castle = eliminated, last standing wins)

---

## 3. Kingdom

A player's realm within a game. Links an AppUser to a Game.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| Name | string | required | Player chosen kingdom name |
| Color | string | required | Hex color e.g. "#e63946", unique per game |
| Status | enum | required | Active, Defeated |
| TurnOrder | int | required | Sequence position (1st, 2nd, 3rd...) |
| FactionTypeId | Guid | required, FK → FactionType | |
| DefeatedAt | DateTime | nullable | When castle was destroyed |
| JoinedAt | DateTime | required | When player joined lobby |
| AppUserId | Guid | required, FK → AppUser | |
| GameId | Guid | required, FK → Game | |

**Relationships:**
- One Kingdom → many Tile (owned tiles)
- One Kingdom → many Building
- One Kingdom → many Army
- One Kingdom → 5 KingdomResource rows
- One Kingdom → many TurnLog
- One Kingdom → many Battle (as attacker or defender)
- One Kingdom → one FactionType

**Business Rules:**
- Color must be unique within a Game — enforced at API level on join
- FactionTypeId must be unique within a Game — one faction per player
- TurnOrder assigned randomly at game start
- Status → Defeated when castle tile is captured
- On defeat: all armies deleted, all buildings destroyed, all tiles become unowned
- One player (AppUser) can only have one Kingdom per Game

---

## 4. Tile

An individual hex cell on the map. Generated at game start.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| Q | int | required | Hex axial coordinate axis 1 |
| R | int | required | Hex axial coordinate axis 2 |
| TerrainTypeId | Guid | required, FK → TerrainType | |
| OwnerKingdomId | Guid | nullable, FK → Kingdom | null = unclaimed |
| GameId | Guid | required, FK → Game | |
| IsCastle | bool | required, default false | Starting tile with castle |
| ClaimedAt | DateTime | nullable | When tile was first claimed |

**Relationships:**
- One Tile → zero or one Building
- Many Tile → one TerrainType
- Many Tile → one Game

**Business Rules:**
- Q + R combination must be unique per Game
- IsCastle = true only for the starting tile of each kingdom
- Castle tiles cannot be rebuilt once captured
- Ownership history is not tracked on Tile — derived from TurnLog instead

---

## 5. TerrainType

Reference/lookup table. Defines the properties of each terrain type. Seeded at startup. **Admin editable via admin panel.**

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| Name | string | required, unique | Plains, Forest, Mountain, Desert, Magic Grove |
| ResourceMultiplier | decimal | required, default 1.10 | Yield bonus when terrain matches building |
| BonusResourceType | string | nullable | Which resource gets multiplier e.g. "Wood" |
| MapColor | string | required | Hex color for map rendering e.g. "#228B22" |
| IconUrl | string | nullable | Icon for UI |

**Seeded Data:**

| Name | Multiplier | Bonus Resource | Color |
|------|------------|----------------|-------|
| Plains | 1.10 | Food | #90EE90 |
| Forest | 1.10 | Wood | #228B22 |
| Mountain | 1.10 | Stone | #808080 |
| Desert | 1.10 | Gold | #C2B280 |
| Magic Grove | 1.10 | Mana | #9B59B6 |

---

## 6. BuildingType

Reference/lookup table. Defines all building templates and the unlock tree. Seeded at startup. **Admin editable via admin panel.**

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| Name | string | required, unique | e.g. "Farm", "Windmill", "Castle" |
| Tier | int | required | 1, 2, or 3 (Castle = special) |
| Chain | string | required | Food, Wood, Stone, Gold, Mana, Military, Castle |
| UnlockedByBuildingTypeId | Guid | nullable, FK → BuildingType | Self-referencing unlock chain |
| BaseYieldGold | int | required, default 0 | Gold produced per turn |
| BaseYieldFood | int | required, default 0 | Food produced per turn |
| BaseYieldWood | int | required, default 0 | Wood produced per turn |
| BaseYieldStone | int | required, default 0 | Stone produced per turn |
| BaseYieldMana | int | required, default 0 | Mana produced per turn |
| ArmyCapacity | int | required, default 0 | Max armies this building supports (military only) |
| CostGold | int | required, default 0 | |
| CostFood | int | required, default 0 | |
| CostWood | int | required, default 0 | |
| CostStone | int | required, default 0 | |
| CostMana | int | required, default 0 | |
| IconUrl | string | nullable | |
| Description | string | nullable | |

**Building Unlock Trees:**

| Chain | Tier 1 | Tier 2 | Tier 3 |
|-------|--------|--------|--------|
| Food | Farm | Windmill | Granary |
| Wood | Lumber Camp | Sawmill | Timber Hall |
| Stone | Quarry | Mason | Stoneworks |
| Gold | Market | Trading Post | Bank |
| Mana | Shrine | Wizard Tower | Arcane Sanctum |
| Military | Barracks | Stables | War Academy |

**Castle:** Special building type, not part of a chain. BaseYieldFood = 10, BaseYieldWood = 10, BaseYieldStone = 10. Cannot be constructed or rebuilt. Placed automatically at game start.

**Business Rules:**
- A building can only be constructed if its UnlockedByBuildingTypeId building already exists on the same tile
- One building per tile (enforced via unique constraint on Building.TileId)
- Military buildings have ArmyCapacity = 3
- When a tile is captured, the building on it is **destroyed**

---

## 7. Building

An instance of a building placed on a tile in a game.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| BuildingTypeId | Guid | required, FK → BuildingType | |
| TileId | Guid | required, unique, FK → Tile | Unique enforces one building per tile |
| KingdomId | Guid | required, FK → Kingdom | |
| BuiltOnRound | int | required | |
| BuiltAt | DateTime | required | |

**Business Rules:**
- TileId is unique — one building per tile enforced at DB level
- KingdomId must match the current owner of the Tile
- When a tile is captured, the building is **destroyed** (hard deleted)
- Destroying a military building also destroys all armies tied to it

---

## 8. ArmyType

Reference/lookup table. Defines all army templates. Seeded at startup. **Admin editable via admin panel.**

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| Name | string | required, unique | Warrior, Scout, Knight, Berserker, Mage, Guardian |
| Attack | int | required | Base damage dealt when winning a round |
| HP | int | required | Health pool — destroyed at 0 |
| Initiative | int | required | Chance weight for winning the round roll |
| DamageRangeMin | decimal | required | Min % of effective Attack dealt on hit (e.g. 0.60) |
| DamageRangeMax | decimal | required | Max % of effective Attack dealt on hit (e.g. 1.00) |
| ChipDamageRangeMin | decimal | required | Min % of loser's effective Attack dealt back (e.g. 0.00) |
| ChipDamageRangeMax | decimal | required | Max % of loser's effective Attack dealt back (e.g. 0.15) |
| SituationalBonusStat | string | nullable | Which stat gets bonus (Attack, HP, Initiative) |
| SituationalBonusValue | decimal | nullable | Bonus multiplier (e.g. 0.10 = +10%) |
| SituationalBonusCondition | enum | nullable | Attacking, Defending |
| TrainingCostGold | int | required, default 0 | |
| TrainingCostFood | int | required, default 0 | |
| TrainingCostStone | int | required, default 0 | |
| TrainingCostMana | int | required, default 0 | |
| UpkeepGold | int | required, default 0 | Per-round cost |
| UpkeepFood | int | required, default 0 | Per-round cost |
| UpkeepMana | int | required, default 0 | Per-round cost |
| RequiredBuildingTypeId | Guid | required, FK → BuildingType | Military building needed |
| IconUrl | string | nullable | |
| Description | string | nullable | |

**Seeded Data:**

| Type | Atk | HP | Init | Dmg Range | Chip Range | Bonus | Required |
|------|-----|----|------|-----------|------------|-------|----------|
| Warrior | 25 | 100 | 50 | 60–100% | 0–15% | +10% Atk defending | Barracks |
| Scout | 15 | 60 | 70 | 50–80% | 0–10% | None | Barracks |
| Knight | 35 | 140 | 30 | 60–100% | 0–15% | +10% HP defending | Stables |
| Berserker | 35 | 60 | 50 | 70–100% | 0–10% | +15% Atk attacking | Stables |
| Mage | 35 | 60 | 70 | 40–100% | 0–5% | +20% Atk attacking | War Academy |
| Guardian | 15 | 140 | 30 | 50–90% | 5–25% | +15% Init defending | War Academy |

---

## 9. Army

A single army entity in a kingdom's global roster. Tied to the military building it was trained from.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| ArmyTypeId | Guid | required, FK → ArmyType | |
| KingdomId | Guid | required, FK → Kingdom | |
| BuildingId | Guid | required, FK → Building | Military building this army is tied to |
| CurrentHP | int | required | Current health, starts at ArmyType.HP (modified by faction) |
| MaxHP | int | required | Max health (ArmyType.HP × faction HP modifier) |
| CreatedAt | DateTime | required | |
| CreatedOnRound | int | required | |

**Relationships:**
- Many Army → one ArmyType
- Many Army → one Kingdom
- Many Army → one Building (military building it was trained from)

**Business Rules:**
- Armies are part of a global roster — not positioned on the map
- Each army is tied to the military building it was trained from
- Each military building can support up to 3 armies (BuildingType.ArmyCapacity)
- If the building is destroyed, all armies tied to it are destroyed
- Army is hard deleted when destroyed in combat (HP reaches 0)
- CurrentHP heals each round during Income Phase (Game.HealPercent × MaxHP)
- effectiveAttack = ArmyType.Attack × (CurrentHP / MaxHP)

---

## 10. Battle

A record of combat between two sides. Created when a battle is resolved.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| GameId | Guid | required, FK → Game | |
| RoundNumber | int | required | Game round this battle occurred in |
| AttackerKingdomId | Guid | required, FK → Kingdom | |
| DefenderKingdomId | Guid | required, FK → Kingdom | |
| AttackerTileId | Guid | required, FK → Tile | Tile the attacker risked |
| DefenderTileId | Guid | required, FK → Tile | Tile the attacker targeted |
| Outcome | enum | required | AttackerWon, DefenderWon |
| TileCapturedId | Guid | required, FK → Tile | The tile that changed ownership |
| TileCapturedFromKingdomId | Guid | required, FK → Kingdom | Kingdom that lost the tile |
| OccurredAt | DateTime | required | Server timestamp |

**Relationships:**
- One Battle → many BattleRound

---

## 11. BattleRound

An individual round within a battle. Records the initiative roll, damage, and chip damage.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| BattleId | Guid | required, FK → Battle | |
| RoundNumber | int | required | Sequential round number within this battle |
| AttackerArmyId | Guid | required, FK → Army | Attacker's active army this round |
| DefenderArmyId | Guid | required, FK → Army | Defender's active army this round |
| InitiativeWinner | enum | required | Attacker, Defender |
| AttackerInitiativeChance | decimal | required | Calculated % chance for attacker |
| DefenderInitiativeChance | decimal | required | Calculated % chance for defender |
| DamageDealt | int | required | Damage applied to the losing army |
| ChipDamageDealt | int | required | Chip damage applied to the winning army |
| AttackerArmyHPAfter | int | required | Attacker's active army HP after this round |
| DefenderArmyHPAfter | int | required | Defender's active army HP after this round |
| ArmyDestroyedId | Guid | nullable, FK → Army | ID of army destroyed this round (null if none) |

---

## 12. KingdomResource

Tracks current resource amounts per kingdom. Each kingdom has exactly 5 rows (one per resource type).

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| KingdomId | Guid | required, FK → Kingdom | |
| ResourceType | enum | required | Gold, Food, Wood, Stone, Mana |
| Amount | int | required, default 0 | Current amount held |
| UpdatedAt | DateTime | required | Last updated timestamp |

**Business Rules:**
- KingdomId + ResourceType must be unique (5 rows per kingdom, one per type)
- Amount cannot go below 0
- Created with starting values when Kingdom is created at game start (base + faction bonus)
- Updated every income phase and whenever resources are spent

**Starting Resources (base, admin-editable):**

| Resource | Base Amount |
|----------|------------|
| Gold | 150 |
| Food | 60 |
| Wood | 50 |
| Stone | 20 |
| Mana | 0 |

---

## 13. TurnLog

Records every action and event that occurred during a round. Acts as the full game history.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| GameId | Guid | required, FK → Game | |
| KingdomId | Guid | nullable, FK → Kingdom | Kingdom this event belongs to (null for game-wide events) |
| RoundNumber | int | required | |
| EventType | enum | required | See EventType list below |
| Description | string | required | Human readable e.g. "Kingdom trained a Warrior army" |
| Metadata | string | nullable | JSON blob for extra details |
| OccurredAt | DateTime | required | |

**EventTypes:**

| EventType | When Created |
|-----------|-------------|
| RoundStarted | Beginning of Action Phase |
| TurnStarted | When a player's action phase begins |
| BuildingConstructed | Action Phase — build |
| BuildingUpgraded | Action Phase — upgrade |
| ArmyTrained | Action Phase — train |
| AttackDeclared | Action Phase — declare attack (tiles only) |
| SlotMachineSpin | Action Phase — slot machine spin |
| ArmySelected | Battle Phase — army selection |
| LineupSet | Battle Phase — army order set |
| BattleResolved | Battle Phase — battle result |
| TileCaptured | Battle Phase — tile ownership changed |
| BuildingDestroyed | Battle Phase — building on captured tile razed |
| ArmyDestroyed | Battle Phase — army killed, or building destroyed |
| ResourcesEarned | Income Phase — resource generation |
| UpkeepPaid | Income Phase — upkeep deducted |
| ArmyDisbanded | Income Phase — upkeep failure |
| ArmyHealed | Income Phase — HP restored |
| KingdomDefeated | Income Phase — castle destroyed |
| GameOver | Income Phase — win condition met or max rounds |
| RoundEnded | Round End |

---

## 14. FactionType

Reference/lookup table. Defines the 4 playable factions with their stat modifiers. Seeded at startup. **Admin editable via admin panel.**

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| Name | string | required, unique | e.g. "Iron Throne" |
| Description | string | required | Short gameplay description |
| Lore | string | nullable | Flavour/story text |
| AttackModifier | decimal | required, default 1.0 | Multiplier on army Attack |
| HPModifier | decimal | required, default 1.0 | Multiplier on army HP |
| InitiativeModifier | decimal | required, default 1.0 | Multiplier on army Initiative |
| ChipDamageModifier | decimal | required, default 1.0 | Multiplier on chip damage ranges |
| ResourceProductionModifier | decimal | required, default 1.0 | Multiplier on building yields |
| BuildingCostModifier | decimal | required, default 1.0 | Multiplier on building costs |
| TrainingCostModifier | decimal | required, default 1.0 | Multiplier on army training costs |
| ActionPointModifier | int | required, default 0 | Added to base action points (+1, -1) |
| HealRateModifier | decimal | required, default 1.0 | Multiplier on global heal rate |
| StartingBonusResource | string | nullable | Which resource gets the starting bonus |
| StartingBonusAmount | int | required, default 0 | How much extra of that resource |
| IconUrl | string | nullable | Faction emblem/icon |

**Seeded Data:**

| Faction | Atk | HP | Init | Chip | ResProd | BldCost | TrnCost | AP | Heal | Starting |
|---------|-----|----|------|------|---------|---------|---------|-----|------|----------|
| Iron Throne | 1.15 | 1.0 | 1.0 | 1.50 | 0.85 | 1.0 | 1.0 | 0 | 1.0 | +50 Gold |
| Mage Council | 1.0 | 0.85 | 1.20 | 1.0 | 1.0 | 1.0 | 1.0 | +1 | 1.0 | +30 Mana |
| Merchant Republic | 0.90 | 1.0 | 1.0 | 1.0 | 1.0 | 0.80 | 0.85 | 0 | 1.0 | +100 Gold |
| Forest Elves | 1.0 | 1.15 | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 | -1 | 1.50 | +50 Wood |

**Business Rules:**
- Each faction can only be chosen by one kingdom per game — enforced at API level on join
- FactionType is picked during lobby join, cannot be changed after game starts
- All modifiers are **multiplicative** (applied as multipliers, not additive)
- ActionPointModifier is the only additive modifier (added to base action points)
- Building cost rounding uses **ceil** (round up) to prevent fractional exploits

---

## Entity Summary

| # | Entity | Type | Count |
|---|--------|------|-------|
| 1 | AppUser | Identity | 1 per registered user |
| 2 | Game | Game State | 1 per match |
| 3 | Kingdom | Game State | 1 per player per game |
| 4 | Tile | Game State | MapWidth × MapHeight per game |
| 5 | TerrainType | Reference | 5 rows (seeded) |
| 6 | BuildingType | Reference | 19 rows (seeded: 18 chain + castle) |
| 7 | Building | Game State | 0–1 per tile |
| 8 | ArmyType | Reference | 6 rows (seeded) |
| 9 | Army | Game State | 0–3 per military building |
| 10 | Battle | Game State | One per battle resolved |
| 11 | BattleRound | Game State | Many per battle |
| 12 | KingdomResource | Game State | 5 per kingdom |
| 13 | TurnLog | History | Many per round |
| 14 | FactionType | Reference | 4 rows (seeded) |

**Total: 14 entities**

### Removed Entities (from original design)
- **UnitType** → replaced by ArmyType (armies are single entities, not squads of units)
- **UnitTypeMatchup** → removed (no rock-paper-scissors matchups)
- **Unit** → removed (armies are single entities with their own HP)
- **GameEvent** → deferred (random events not yet designed)
