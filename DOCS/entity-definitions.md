# Realms of Ash — Entity Definitions

## Overview

16 entities total. Identity tables (AppUser extends IdentityUser), lookup/reference tables (TerrainType, BuildingType, UnitType, UnitTypeMatchup, FactionType), and game state tables (Game, Kingdom, Tile, Building, Army, Unit, Battle, KingdomResource, TurnLog, GameEvent).

---

## Roles

Two roles provided by ASP.NET Core Identity:

| Role | Assigned | Capabilities |
|------|----------|-------------|
| `Admin` | Manually by server owner | Full CRUD on all reference data via admin panel. Manage users and role assignments. |
| `Player` | Automatically on registration | Create and join games, play. No access to admin panel. |

All reference table CRUD controllers (BuildingType, UnitType, TerrainType, UnitTypeMatchup, FactionType) are secured with `[Authorize(Roles = "Admin")]`. This gives admins live control over game balance, faction modifiers, and icon URLs without code changes.

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
| WinCondition | enum | required | Domination, Elimination, Score |
| MaxPlayers | int | required | 2–8, set by creator |
| TurnNumber | int | required, default 0 | Current turn |
| CurrentTurnKingdomId | Guid | nullable, FK → Kingdom | Whose turn it is |
| MapWidth | int | required | Hex grid width |
| MapHeight | int | required | Hex grid height |
| DominationThreshold | int | nullable | % of tiles needed (if WinCondition = Domination) |
| MaxTurns | int | nullable | Max turns (if WinCondition = Score) |
| TurnTimeLimit | int | nullable | Seconds per turn, null = unlimited |
| TurnDeadline | DateTime | nullable | When current turn expires |
| CreatedByUserId | Guid | required, FK → AppUser | Game creator |
| WinnerKingdomId | Guid | nullable, FK → Kingdom | Set when game finishes |
| CreatedAt | DateTime | required | |
| StartedAt | DateTime | nullable | When Status → Active |
| FinishedAt | DateTime | nullable | When Status → Finished |

**Relationships:**
- One Game → many Kingdom
- One Game → many Tile
- One Game → many TurnLog
- One Game → many Battle
- One Game → many GameEvent

**Business Rules:**
- TurnDeadline = StartedAt + TurnTimeLimit each time a new turn begins (if TurnTimeLimit is set)
- Game moves to Active when creator starts it (minimum 2 players must have joined)
- DominationThreshold required if WinCondition = Domination
- MaxTurns required if WinCondition = Score

---

## 3. Kingdom

A player's realm within a game. Links an AppUser to a Game. A special barbarian kingdom (IsBarbarianCamp = true) is also created per game to own neutral enemy armies.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| Name | string | required | Player chosen kingdom name |
| Color | string | required | Hex color e.g. "#e63946", unique per game |
| Status | enum | required | Active, Defeated |
| TurnOrder | int | required | Sequence position (1st, 2nd, 3rd...) |
| IsBarbarianCamp | bool | required, default false | True for the neutral barbarian kingdom |
| FactionTypeId | Guid | nullable, FK → FactionType | null for barbarian kingdom |
| DefeatedAt | DateTime | nullable | When last tile was lost |
| JoinedAt | DateTime | required | When player joined lobby |
| AppUserId | Guid | nullable, FK → AppUser | null for barbarian kingdom |
| GameId | Guid | required, FK → Game | |

**Relationships:**
- One Kingdom → many Tile (owned tiles)
- One Kingdom → many Building
- One Kingdom → many Army
- One Kingdom → many Unit
- One Kingdom → 5 KingdomResource rows (player kingdoms only)
- One Kingdom → many TurnLog
- One Kingdom → many Battle (as attacker or defender)
- One Kingdom → one FactionType (player kingdoms only)

**Business Rules:**
- Color must be unique within a Game — enforced at API level on join
- TurnOrder assigned randomly at game start (barbarian kingdom has TurnOrder = 0, never takes a turn)
- Status → Defeated when kingdom has no remaining owned tiles
- One player (AppUser) can only have one Kingdom per Game
- Barbarian kingdom: IsBarbarianCamp = true, AppUserId = null, FactionTypeId = null
- When a barbarian army is defeated, tile OwnerKingdomId → null (unclaimed, not transferred to attacker directly)

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
| HasSettlement | bool | required, default false | Starting tile marker |
| ClaimedAt | DateTime | nullable | When tile was first claimed |

**Relationships:**
- One Tile → zero or one Building
- One Tile → zero or one Army (per kingdom)
- Many Tile → one TerrainType
- Many Tile → one Game

**Business Rules:**
- Q + R combination must be unique per Game
- Tile can only be claimed if adjacent to an already owned tile (checked in API)
- HasSettlement = true only for the starting tile of each kingdom
- Ownership history is not tracked on Tile — derived from TurnLog instead

---

## 5. TerrainType

Reference/lookup table. Defines the properties of each terrain type. Seeded at startup. **Admin editable via admin panel.**

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| Name | string | required, unique | Plains, Forest, Mountain, River, Magic Grove |
| DefenseBonus | decimal | required | e.g. 0.4 = +40% defense |
| MovementCost | int | required | 1, 2, or 3 |
| ResourceMultiplier | decimal | required, default 1.0 | e.g. 1.2 = +20% yield |
| BonusResourceType | string | nullable | Which resource gets multiplier e.g. "Wood" |
| MapColor | string | required | Hex color for map rendering e.g. "#228B22" |
| IconUrl | string | nullable | Icon for UI |

**Seeded Data:**

| Name | Defense | Movement | Multiplier | Bonus Resource | Color |
|------|---------|----------|------------|----------------|-------|
| Plains | 0.0 | 1 | 1.0 | null | #90EE90 |
| Forest | 0.2 | 2 | 1.2 | Wood | #228B22 |
| Mountain | 0.4 | 3 | 1.4 | Stone | #808080 |
| River | 0.1 | 2 | 1.2 | Gold | #4169E1 |
| Magic Grove | 0.1 | 1 | 1.3 | Mana | #9B59B6 |

---

## 6. BuildingType

Reference/lookup table. Defines all building templates and the unlock tree. Seeded at startup. **Admin editable via admin panel — balance values and icon URLs can be changed live.**

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| Name | string | required, unique | e.g. "Farm", "Windmill" |
| Tier | int | required | 1, 2, or 3 |
| UnlockedByBuildingTypeId | Guid | nullable, FK → BuildingType | Self-referencing unlock chain |
| ResourceProduced | string | required | e.g. "Food" |
| BaseYield | int | required | Base amount produced per turn |
| CostGold | int | required, default 0 | |
| CostFood | int | required, default 0 | |
| CostWood | int | required, default 0 | |
| CostStone | int | required, default 0 | |
| CostMana | int | required, default 0 | |
| RequiredTerrain | string | nullable | If restricted to terrain type |
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
| Defense | Palisade | Stone Wall | Fortress |

**Business Rules:**
- A building can only be constructed if its UnlockedByBuildingTypeId building already exists on the same tile
- One building per tile (enforced via unique constraint on Building.TileId)

---

## 7. Building

An instance of a building placed on a tile in a game.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| BuildingTypeId | Guid | required, FK → BuildingType | |
| TileId | Guid | required, unique, FK → Tile | Unique enforces one building per tile |
| KingdomId | Guid | required, FK → Kingdom | |
| BuiltOnTurn | int | required | |
| BuiltAt | DateTime | required | |

**Business Rules:**
- TileId is unique — one building per tile enforced at DB level
- KingdomId must match the current owner of the Tile
- When a tile is captured, buildings remain on the tile (new owner benefits from them)

---

## 8. UnitType

Reference/lookup table. Defines all unit templates. Seeded at startup. **Admin editable via admin panel — balance values and icon URLs can be changed live.**

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| Name | string | required, unique | Swordsman, Archer, Knight, Mage, Catapult |
| IsMagical | bool | required | If true, costs and upkeep use Mana |
| BaseStrength | int | required | Base combat power value |
| RecruitCostGold | int | required, default 0 | |
| RecruitCostFood | int | required, default 0 | |
| RecruitCostWood | int | required, default 0 | |
| RecruitCostStone | int | required, default 0 | |
| RecruitCostMana | int | required, default 0 | |
| UpkeepGold | int | required, default 0 | Cost per turn to maintain |
| UpkeepFood | int | required, default 0 | Cost per turn to maintain |
| RequiredBuildingTypeId | Guid | required, FK → BuildingType | Building needed to recruit |
| IconUrl | string | nullable | |
| Description | string | nullable | |

**Seeded Data:**

| Unit | Strength | Requires | Magical |
|------|----------|----------|---------|
| Swordsman | 10 | Barracks | No |
| Archer | 8 | Barracks | No |
| Knight | 15 | Stables | No |
| Mage | 12 | Wizard Tower | Yes |
| Catapult | 20 | War Academy | No |

---

## 9. UnitTypeMatchup

Stores the rock-paper-scissors combat matchup table. Seeded at startup. **Admin editable via admin panel — multipliers can be tuned for balance.**

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| AttackerUnitTypeId | Guid | required, FK → UnitType | |
| DefenderUnitTypeId | Guid | required, FK → UnitType | |
| DamageMultiplier | decimal | required | 1.25 = strong, 0.75 = weak, 1.0 = neutral |

**Seeded Matchups:**

| Attacker | Defender | Multiplier |
|----------|----------|------------|
| Swordsman | Archer | 1.25 |
| Swordsman | Knight | 0.75 |
| Swordsman | Mage | 1.25 |
| Archer | Knight | 1.25 |
| Archer | Swordsman | 0.75 |
| Archer | Mage | 0.75 |
| Knight | Mage | 1.25 |
| Knight | Swordsman | 1.25 |
| Knight | Archer | 0.75 |
| Mage | Archer | 1.25 |
| Mage | Swordsman | 0.75 |
| Mage | Knight | 0.75 |
| Catapult | (all) | 1.0 |

**Business Rules:**
- If no matchup row exists for a pair, default multiplier = 1.0
- AttackerUnitTypeId + DefenderUnitTypeId combination must be unique

---

## 10. Army

A group of units positioned on a tile. A kingdom can have multiple armies on different tiles.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| KingdomId | Guid | required, FK → Kingdom | |
| TileId | Guid | required, FK → Tile | |
| CreatedAt | DateTime | required | |
| CreatedOnTurn | int | required | |

**Relationships:**
- One Army → many Unit
- Unique constraint on (KingdomId + TileId) — one army per kingdom per tile

**Business Rules:**
- Moving an army onto a tile where the same kingdom already has an army merges them
- Army is deleted when all its units are destroyed in battle
- Army can only be on a tile owned by its Kingdom (except during attack resolution)

---

## 11. Unit

An individual unit belonging to an army.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| UnitTypeId | Guid | required, FK → UnitType | |
| ArmyId | Guid | required, FK → Army | |
| KingdomId | Guid | required, FK → Kingdom | |
| RecruitedAt | DateTime | required | |
| RecruitedOnTurn | int | required | |

**Business Rules:**
- Units are deleted (hard delete) when killed in battle — recorded in Battle entity
- Upkeep is paid per unit per turn during income phase
- If a kingdom cannot afford upkeep, units are disbanded (deleted) starting from most expensive

---

## 12. Battle

A record of combat between two armies. Created when an attack action is taken.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| GameId | Guid | required, FK → Game | |
| TurnNumber | int | required | |
| AttackerKingdomId | Guid | required, FK → Kingdom | |
| DefenderKingdomId | Guid | required, FK → Kingdom | |
| TileId | Guid | required, FK → Tile | Tile being contested |
| AttackerPowerTotal | decimal | required | Final attacker power after all modifiers |
| DefenderPowerTotal | decimal | required | Final defender power after all modifiers |
| AttackerDiceRoll | decimal | required | 0.85–1.15 random roll |
| DefenderDiceRoll | decimal | required | 0.85–1.15 random roll |
| AttackerUnitsLost | int | required | |
| DefenderUnitsLost | int | required | |
| Outcome | enum | required | AttackerWon, DefenderWon |
| TileChangedOwner | bool | required | Whether attacker captured the tile |
| OccurredAt | DateTime | required | |

**Combat Formula:**
```
EffectivePower = Σ(unit.BaseStrength × matchupModifier) × terrainBonus × diceRoll
```
- matchupModifier from UnitTypeMatchup table
- terrainBonus from TerrainType.DefenseBonus (defender only)
- diceRoll = random decimal between 0.85 and 1.15

---

## 13. KingdomResource

Tracks current resource amounts per kingdom. Each kingdom has exactly 5 rows (one per resource type).

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| KingdomId | Guid | required, FK → Kingdom | |
| ResourceType | enum | required | Gold, Food, Wood, Stone, Mana |
| Amount | decimal | required, default 0 | Current amount held |
| UpdatedAt | DateTime | required | Last updated timestamp |

**Business Rules:**
- KingdomId + ResourceType must be unique (5 rows per kingdom, one per type)
- Amount cannot go below 0
- Created with Amount = starting values when Kingdom is created at game start
- Updated every income phase and whenever resources are spent

---

## 14. TurnLog

Records every action and event that occurred during a turn. Acts as the full game history.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| GameId | Guid | required, FK → Game | |
| KingdomId | Guid | required, FK → Kingdom | Kingdom this event belongs to |
| TurnNumber | int | required | |
| EventType | enum | required | TileClaimed, BuildingConstructed, UnitRecruited, ArmyMoved, BattleOccurred, ResourcesEarned, UpkeepPaid, TurnStarted, TurnEnded, KingdomDefeated |
| Description | string | required | Human readable e.g. "Swordsman army attacked Forest tile at Q3 R5" |
| Metadata | string | nullable | JSON blob for extra details e.g. BattleId, TileId, amount |
| OccurredAt | DateTime | required | |

**Business Rules:**
- Tile ownership history is derived from TurnLog (EventType = TileClaimed / BattleOccurred) rather than stored on Tile
- Metadata stores structured JSON for UI to link to related entities e.g. `{"battleId": "...", "tileQ": 3, "tileR": 5}`

---

## 15. GameEvent

Random events that fire during the game affecting kingdoms or tiles.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| GameId | Guid | required, FK → Game | |
| KingdomId | Guid | nullable, FK → Kingdom | null = affects all kingdoms |
| TileId | Guid | nullable, FK → Tile | null = kingdom-wide effect |
| TurnNumber | int | required | Turn it fired on |
| EventType | enum | required | Plague, GoodHarvest, DragonAttack, MagicStorm, GoldRush, Drought |
| Description | string | required | Human readable description |
| ResourceEffect | string | nullable | e.g. "Food:-20" or "Gold:+50" |
| OccurredAt | DateTime | required | |

**Business Rules:**
- Events fire at the end of the income phase each turn
- Probability and targeting rules are handled in game logic, not stored in DB
- KingdomId = null means the event affects all kingdoms in the game equally

---

## 16. FactionType

Reference/lookup table. Defines the 4 playable factions with their stat modifiers. Seeded at startup. **Admin editable via admin panel — modifiers and icons can be tuned live.**

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| Id | Guid | PK | |
| Name | string | required, unique | e.g. "Iron Throne" |
| Description | string | required | Short gameplay description |
| Lore | string | nullable | Flavour/story text |
| ResourceProductionBonus | decimal | required, default 1.0 | Multiplier on resource income |
| BonusResourceType | string | nullable | Which resource gets the bonus, null = all |
| UnitStrengthBonus | decimal | required, default 1.0 | Multiplier on unit BaseStrength |
| BonusUnitType | string | nullable | Which unit type gets the bonus, null = all |
| BuildingCostModifier | decimal | required, default 1.0 | Multiplier on all building costs |
| StartingGold | int | required, default 0 | Bonus gold at game start |
| StartingFood | int | required, default 0 | Bonus food at game start |
| StartingWood | int | required, default 0 | Bonus wood at game start |
| StartingStone | int | required, default 0 | Bonus stone at game start |
| StartingMana | int | required, default 0 | Bonus mana at game start |
| IconUrl | string | nullable | Faction emblem/icon |

**Seeded Data:**

| Faction | Resource Bonus | Unit Bonus | Building Cost | Starting Bonus |
|---------|---------------|------------|---------------|----------------|
| Iron Throne | — | +20% all units | +10% (costs more) | +50 Gold |
| Mage Council | +20% Mana only | +20% Mage only | -10% | +30 Mana |
| Merchant Republic | +30% Gold only | -10% all units | -20% | +100 Gold |
| Forest Elves | +20% Food & Wood | — | -10% | +50 Wood |

**Business Rules:**
- Each faction can only be chosen by one kingdom per game — enforced at API level on join
- FactionType is picked during lobby join, cannot be changed after game starts
- Modifiers apply passively every turn — no code changes needed when admin tunes values

---

## Entity Summary

| # | Entity | Type | Count |
|---|--------|------|-------|
| 1 | AppUser | Identity | 1 |
| 2 | Game | Game State | 1 |
| 3 | Kingdom | Game State | 1 per player + 1 barbarian per game |
| 4 | Tile | Game State | MapWidth × MapHeight per game |
| 5 | TerrainType | Reference | 5 rows (seeded) |
| 6 | BuildingType | Reference | 21 rows (seeded) |
| 7 | Building | Game State | 0–1 per tile |
| 8 | UnitType | Reference | 5 rows (seeded) |
| 9 | UnitTypeMatchup | Reference | ~20 rows (seeded) |
| 10 | Army | Game State | 0–1 per kingdom per tile |
| 11 | Unit | Game State | Many per army |
| 12 | Battle | Game State | One per attack action |
| 13 | KingdomResource | Game State | 5 per player kingdom |
| 14 | TurnLog | History | Many per turn per kingdom |
| 15 | GameEvent | History | 0–N per turn |
| 16 | FactionType | Reference | 4 rows (seeded) |

**Total: 16 entities** (well above the required minimum of 10)
