# Backend Database Schema

Database: **PostgreSQL** via Entity Framework Core (`Backend/Infrastructure/AppDbContext.cs`).
All entities inherit from `BaseEntity` (`Id: Guid`, `CreatedAt`, `UpdatedAt`).
Identity tables (`AspNetUsers`, `AspNetRoles`, `AppRefreshToken`, `DataProtectionKeys`, …) come from `IdentityDbContext` and are shown only at the boundary where domain entities reference them.

## Visual ERD

Two SVG diagrams generated from the live PostgreSQL schema (open in a browser to zoom):

- [`db-schema.svg`](db-schema.svg) — full ERD with **all columns** visible (the canonical visual)
- [`db-schema-compact.svg`](db-schema-compact.svg) — **PK/FK only**, easier to skim at a glance

To regenerate after schema changes (requires the dev DB to be running and migrated):

```bash
docker run --rm --network host \
  -v "$PWD/DOCS/schemaspy:/output" \
  schemaspy/schemaspy:latest \
  -t pgsql11 -host localhost -port 5432 \
  -db postgres-dev -u postgres -p postgres -s public -vizjs \
  -I "__EFMigrationsHistory|DataProtectionKeys"

cp DOCS/schemaspy/diagrams/summary/relationships.real.large.svg   DOCS/db-schema.svg
cp DOCS/schemaspy/diagrams/summary/relationships.real.compact.svg DOCS/db-schema-compact.svg
rm -rf DOCS/schemaspy
```

The Mermaid block below is the inline-readable, hand-annotated version — it carries prose notes (cascade-delete behavior, `xmin` row version, historical FK-less references) that the auto-generated SVGs don't.

> Diagram conventions: `PK` = primary key, `FK` = foreign key. Unique indexes are noted in the inline comment (e.g. `"unique"`) and listed in full under **Notes** below. `||--o{` = one-to-many; `||--o|` = one-to-(zero-or-)one. Reference / lookup tables are grouped at the bottom.

## Entity Relationships

```mermaid
erDiagram
    %% ============ Identity (boundary) ============
    AspNetUsers {
        guid Id PK
        string UserName
        string Email
    }

    %% ============ Game core ============
    Games {
        guid Id PK
        string Name
        string Status "enum as string"
        int RoundNumber
        string CurrentPhase "enum as string"
        string WinCondition "enum as string"
        int MaxPlayers "2-4"
        string LobbyCode "unique"
        int MapWidth
        int MapHeight
        guid HostUserId FK "AspNetUsers, nullable"
        guid CurrentTurnKingdomId FK "Kingdoms, nullable"
        guid WinnerKingdomId FK "Kingdoms, nullable"
        int MaxRounds
        int BaseActionPoints
        decimal HealPercent
        int SpinCostGold
        jsonb SlotOutcomeWeights
        int TurnTimeLimit "nullable"
        datetime TurnDeadline "nullable"
        int RemainingActionPoints "nullable"
        datetime StartedAt "nullable"
        datetime FinishedAt "nullable"
        uint xmin "row version"
        datetime CreatedAt
        datetime UpdatedAt
    }

    Kingdoms {
        guid Id PK
        string Name
        string Color
        string Status "enum as string"
        int TurnOrder
        guid GameId FK
        guid AppUserId FK "AspNetUsers, nullable"
        guid FactionTypeId FK "nullable"
        datetime DefeatedAt "nullable"
        datetime JoinedAt
        int ConsecutiveMissedTurns
        datetime CreatedAt
        datetime UpdatedAt
    }

    KingdomResources {
        guid Id PK
        guid KingdomId FK "unique with ResourceType"
        string ResourceType "enum as string; unique with KingdomId"
        decimal Amount "precision 18,2"
        datetime CreatedAt
        datetime UpdatedAt
    }

    TurnLogs {
        guid Id PK
        int RoundNumber
        string EventType "enum as string"
        string Description
        jsonb Metadata "nullable"
        datetime OccurredAt
        guid GameId FK
        guid KingdomId FK "nullable"
        datetime CreatedAt
        datetime UpdatedAt
    }

    GameEvents {
        guid Id PK
        string Name
        string Description "nullable"
        decimal ResourceEffect
        datetime CreatedAt
        datetime UpdatedAt
    }

    %% ============ Map ============
    Tiles {
        guid Id PK
        int CoordQ "unique with GameId+CoordR"
        int CoordR "unique with GameId+CoordQ"
        guid GameId FK "unique with CoordQ+CoordR"
        guid TerrainTypeId FK
        guid KingdomId FK "nullable - unclaimed"
        bool IsCastle "default false"
        datetime CreatedAt
        datetime UpdatedAt
    }

    %% ============ Buildings ============
    Buildings {
        guid Id PK
        guid TileId FK "unique - one building per tile"
        guid BuildingTypeId FK
        guid KingdomId FK
        int BuiltOnRound
        datetime BuiltAt
        datetime CreatedAt
        datetime UpdatedAt
    }

    %% ============ Military ============
    Armies {
        guid Id PK
        guid ArmyTypeId FK
        guid KingdomId FK "indexed"
        guid BuildingId FK "indexed"
        int CurrentHP
        int MaxHP
        int CreatedOnRound
        datetime CreatedAt
        datetime UpdatedAt
    }

    Battles {
        guid Id PK
        guid GameId FK
        int RoundNumber
        guid AttackerKingdomId FK
        guid DefenderKingdomId FK
        guid AttackerTileId FK
        guid DefenderTileId FK
        string Outcome "enum as string"
        guid TileCapturedId FK
        guid TileCapturedFromKingdomId FK
        datetime OccurredAt
        datetime CreatedAt
        datetime UpdatedAt
    }

    BattleRounds {
        guid Id PK
        guid BattleId FK "unique with RoundNumber"
        int RoundNumber "unique with BattleId"
        guid AttackerArmyId "historical, no FK"
        guid DefenderArmyId "historical, no FK"
        string InitiativeWinner "enum as string"
        decimal AttackerInitiativeChance
        decimal DefenderInitiativeChance
        int DamageDealt
        int ChipDamageDealt
        int AttackerArmyHPAfter
        int DefenderArmyHPAfter
        guid ArmyDestroyedId "nullable, historical"
        datetime CreatedAt
        datetime UpdatedAt
    }

    DeclaredAttacks {
        guid Id PK
        guid GameId FK "cascade delete"
        int RoundNumber
        guid AttackerKingdomId FK
        guid DefenderKingdomId FK
        guid TargetTileId FK
        guid RiskedTileId FK
        string AttackerSelectedArmyIds "csv guids, nullable"
        string DefenderSelectedArmyIds "csv guids, nullable"
        bool AttackerLineupConfirmed
        bool DefenderLineupConfirmed
        datetime CreatedAt
        datetime UpdatedAt
    }

    %% ============ Reference / Lookup ============
    TerrainTypes {
        guid Id PK
        jsonb Name "LangStr"
        decimal ResourceMultiplier
        string ResourceBonusType "enum as string"
        string MapColor
        string IconUrl "nullable"
        datetime CreatedAt
        datetime UpdatedAt
    }

    BuildingTypes {
        guid Id PK
        jsonb Name "LangStr"
        int Tier
        string Chain
        int CostGold
        int CostFood
        int CostWood
        int CostStone
        int CostMana
        int BaseYieldGold
        int BaseYieldFood
        int BaseYieldWood
        int BaseYieldStone
        int BaseYieldMana
        int ArmyCapacity
        jsonb Description "LangStr"
        string IconUrl "nullable"
        guid UnlockedByBuildingTypeId FK "self, nullable"
        datetime CreatedAt
        datetime UpdatedAt
    }

    ArmyTypes {
        guid Id PK
        jsonb Name "LangStr"
        int Attack
        int HP
        int Initiative
        decimal DamageRangeMin
        decimal DamageRangeMax
        decimal ChipDamageRangeMin
        decimal ChipDamageRangeMax
        string SituationalBonusStat "nullable"
        decimal SituationalBonusValue "nullable"
        string SituationalBonusCondition "enum as string, nullable"
        int TrainingCostGold
        int TrainingCostFood
        int TrainingCostStone
        int TrainingCostMana
        int UpkeepGold
        int UpkeepFood
        int UpkeepMana
        guid RequiredBuildingTypeId FK
        string IconUrl "nullable"
        jsonb Description "LangStr"
        datetime CreatedAt
        datetime UpdatedAt
    }

    FactionTypes {
        guid Id PK
        jsonb Name "LangStr"
        jsonb Description "LangStr"
        jsonb Lore "LangStr"
        decimal AttackModifier
        decimal HPModifier
        decimal InitiativeModifier
        decimal ChipDamageModifier
        decimal ResourceProductionModifier
        decimal BuildingCostModifier
        decimal TrainingCostModifier
        int ActionPointModifier
        decimal HealRateModifier
        string StartingBonusResource "enum as string, nullable"
        int StartingBonusAmount
        string IconUrl "nullable"
        datetime CreatedAt
        datetime UpdatedAt
    }

    %% ============ Relationships ============

    %% Game ↔ identity / kingdoms
    AspNetUsers ||--o{ Games          : "hosts"
    AspNetUsers ||--o{ Kingdoms       : "plays as"
    Games       ||--o{ Kingdoms       : "has"
    Games       ||--o| Kingdoms       : "current turn"
    Games       ||--o| Kingdoms       : "winner"

    %% Game ↔ map / logs / battles
    Games       ||--o{ Tiles          : "contains"
    Games       ||--o{ TurnLogs       : "logs"
    Games       ||--o{ Battles        : "hosts"
    Games       ||--o{ DeclaredAttacks: "queues"

    %% Kingdom ↔ owned things
    Kingdoms    ||--o{ Tiles          : "owns (nullable)"
    Kingdoms    ||--o{ Buildings      : "owns"
    Kingdoms    ||--o{ Armies         : "fields"
    Kingdoms    ||--o{ KingdomResources: "stockpiles"
    Kingdoms    ||--o{ TurnLogs       : "subject of"
    FactionTypes||--o{ Kingdoms       : "identity"

    %% Map / buildings / armies
    TerrainTypes||--o{ Tiles          : "type of"
    Tiles       ||--o| Buildings      : "hosts (1:1)"
    BuildingTypes ||--o{ Buildings    : "type of"
    BuildingTypes ||--o{ BuildingTypes: "unlocked by (chain)"
    BuildingTypes ||--o{ ArmyTypes    : "required for"
    Buildings   ||--o{ Armies         : "garrisons"
    ArmyTypes   ||--o{ Armies         : "type of"

    %% Battles
    Kingdoms    ||--o{ Battles        : "attacker"
    Kingdoms    ||--o{ Battles        : "defender"
    Kingdoms    ||--o{ Battles        : "lost tile from"
    Tiles       ||--o{ Battles        : "attacker tile"
    Tiles       ||--o{ Battles        : "defender tile"
    Tiles       ||--o{ Battles        : "captured tile"
    Battles     ||--o{ BattleRounds   : "rounds"

    %% Declared attacks
    Kingdoms    ||--o{ DeclaredAttacks: "attacker"
    Kingdoms    ||--o{ DeclaredAttacks: "defender"
    Tiles       ||--o{ DeclaredAttacks: "target tile"
    Tiles       ||--o{ DeclaredAttacks: "risked tile"
```

## Notes

- **Cascade deletes are disabled globally** in `OnModelCreating` (`DeleteBehavior.Restrict`). The only explicit cascade is `DeclaredAttacks → Games`.
- **Enums are persisted as strings** via `HasConversion<string>()` for `Game.Status`, `Game.WinCondition`, `Game.CurrentPhase`, `Kingdom.Status`, `TurnLog.EventType`, `Battle.Outcome`, `BattleRound.InitiativeWinner`, `ArmyType.SituationalBonusCondition`, `FactionType.StartingBonusResource`, `KingdomResource.ResourceType`, and `TerrainType.ResourceBonusType`.
- **Localized strings** (`LangStr`) on lookup tables are stored as `jsonb` columns.
- **Concurrency:** `Games.xmin` is mapped to PostgreSQL's system row-version column to guard against lobby-join races.
- **Unique indexes:**
  - `Games.LobbyCode`
  - `Tiles (GameId, CoordQ, CoordR)`
  - `KingdomResources (KingdomId, ResourceType)`
  - `Buildings.TileId` — enforces one building per tile
  - `BattleRounds (BattleId, RoundNumber)`
- **Non-unique indexes** for hot query paths: `TurnLogs (GameId, RoundNumber)`, `TurnLogs (GameId, KingdomId)`, `Armies.BuildingId`, `Armies.KingdomId`.
- **Historical references:** `BattleRound.AttackerArmyId`, `DefenderArmyId`, and `ArmyDestroyedId` are plain `Guid` columns *without* FK constraints — armies may be destroyed and removed during combat, but the round log must still reference them.
- **CSV-encoded lineups:** `DeclaredAttack.AttackerSelectedArmyIds` / `DefenderSelectedArmyIds` store comma-separated GUID lists; not relational FKs.
- **`HostUserId` and `Kingdom.AppUserId`** are FK columns to `AspNetUsers` configured *without* navigation properties — Domain has no reference to the Identity layer.
- **`SaveChangesAsync` auto-stamps** `CreatedAt` / `UpdatedAt` on every entity implementing `IBaseEntity`. All `DateTime` columns are forced to UTC via value converters because `timestamp with time zone` requires UTC.
```
