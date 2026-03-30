using System;
using Base;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BuildingTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<LangStr>(type: "jsonb", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    Chain = table.Column<string>(type: "text", nullable: false),
                    CostGold = table.Column<int>(type: "integer", nullable: false),
                    CostFood = table.Column<int>(type: "integer", nullable: false),
                    CostWood = table.Column<int>(type: "integer", nullable: false),
                    CostStone = table.Column<int>(type: "integer", nullable: false),
                    CostMana = table.Column<int>(type: "integer", nullable: false),
                    BaseYieldGold = table.Column<int>(type: "integer", nullable: false),
                    BaseYieldFood = table.Column<int>(type: "integer", nullable: false),
                    BaseYieldWood = table.Column<int>(type: "integer", nullable: false),
                    BaseYieldStone = table.Column<int>(type: "integer", nullable: false),
                    BaseYieldMana = table.Column<int>(type: "integer", nullable: false),
                    ArmyCapacity = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<LangStr>(type: "jsonb", nullable: false),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    UnlockedByBuildingTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuildingTypes_BuildingTypes_UnlockedByBuildingTypeId",
                        column: x => x.UnlockedByBuildingTypeId,
                        principalTable: "BuildingTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FactionTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<LangStr>(type: "jsonb", nullable: false),
                    Description = table.Column<LangStr>(type: "jsonb", nullable: false),
                    Lore = table.Column<LangStr>(type: "jsonb", nullable: false),
                    AttackModifier = table.Column<decimal>(type: "numeric", nullable: false),
                    HPModifier = table.Column<decimal>(type: "numeric", nullable: false),
                    InitiativeModifier = table.Column<decimal>(type: "numeric", nullable: false),
                    ChipDamageModifier = table.Column<decimal>(type: "numeric", nullable: false),
                    ResourceProductionModifier = table.Column<decimal>(type: "numeric", nullable: false),
                    BuildingCostModifier = table.Column<decimal>(type: "numeric", nullable: false),
                    TrainingCostModifier = table.Column<decimal>(type: "numeric", nullable: false),
                    ActionPointModifier = table.Column<int>(type: "integer", nullable: false),
                    HealRateModifier = table.Column<decimal>(type: "numeric", nullable: false),
                    StartingBonusResource = table.Column<string>(type: "text", nullable: true),
                    StartingBonusAmount = table.Column<int>(type: "integer", nullable: false),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ResourceEffect = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TerrainTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<LangStr>(type: "jsonb", nullable: false),
                    ResourceMultiplier = table.Column<decimal>(type: "numeric", nullable: false),
                    ResourceBonusType = table.Column<string>(type: "text", nullable: false),
                    MapColor = table.Column<string>(type: "text", nullable: false),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerrainTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousRefreshToken = table.Column<string>(type: "text", nullable: true),
                    PreviousExpiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArmyTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<LangStr>(type: "jsonb", nullable: false),
                    Attack = table.Column<int>(type: "integer", nullable: false),
                    HP = table.Column<int>(type: "integer", nullable: false),
                    Initiative = table.Column<int>(type: "integer", nullable: false),
                    DamageRangeMin = table.Column<decimal>(type: "numeric", nullable: false),
                    DamageRangeMax = table.Column<decimal>(type: "numeric", nullable: false),
                    ChipDamageRangeMin = table.Column<decimal>(type: "numeric", nullable: false),
                    ChipDamageRangeMax = table.Column<decimal>(type: "numeric", nullable: false),
                    SituationalBonusStat = table.Column<string>(type: "text", nullable: true),
                    SituationalBonusValue = table.Column<decimal>(type: "numeric", nullable: true),
                    SituationalBonusCondition = table.Column<string>(type: "text", nullable: true),
                    TrainingCostGold = table.Column<int>(type: "integer", nullable: false),
                    TrainingCostFood = table.Column<int>(type: "integer", nullable: false),
                    TrainingCostStone = table.Column<int>(type: "integer", nullable: false),
                    TrainingCostMana = table.Column<int>(type: "integer", nullable: false),
                    UpkeepGold = table.Column<int>(type: "integer", nullable: false),
                    UpkeepFood = table.Column<int>(type: "integer", nullable: false),
                    UpkeepMana = table.Column<int>(type: "integer", nullable: false),
                    RequiredBuildingTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<LangStr>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArmyTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArmyTypes_BuildingTypes_RequiredBuildingTypeId",
                        column: x => x.RequiredBuildingTypeId,
                        principalTable: "BuildingTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Armies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArmyTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    KingdomId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentHP = table.Column<int>(type: "integer", nullable: false),
                    MaxHP = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnRound = table.Column<int>(type: "integer", nullable: false),
                    TileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Armies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Armies_ArmyTypes_ArmyTypeId",
                        column: x => x.ArmyTypeId,
                        principalTable: "ArmyTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BattleRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BattleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    AttackerArmyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenderArmyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiativeWinner = table.Column<string>(type: "text", nullable: false),
                    AttackerInitiativeChance = table.Column<decimal>(type: "numeric", nullable: false),
                    DefenderInitiativeChance = table.Column<decimal>(type: "numeric", nullable: false),
                    DamageDealt = table.Column<int>(type: "integer", nullable: false),
                    ChipDamageDealt = table.Column<int>(type: "integer", nullable: false),
                    AttackerArmyHPAfter = table.Column<int>(type: "integer", nullable: false),
                    DefenderArmyHPAfter = table.Column<int>(type: "integer", nullable: false),
                    ArmyDestroyedId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleRounds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Battles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    AttackerKingdomId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenderKingdomId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttackerTileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenderTileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    TileCapturedId = table.Column<Guid>(type: "uuid", nullable: false),
                    TileCapturedFromKingdomId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Battles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TileId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    KingdomId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuiltOnRound = table.Column<int>(type: "integer", nullable: false),
                    BuiltAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Buildings_BuildingTypes_BuildingTypeId",
                        column: x => x.BuildingTypeId,
                        principalTable: "BuildingTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeclaredAttacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    AttackerKingdomId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenderKingdomId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetTileId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskedTileId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttackerSelectedArmyIds = table.Column<string>(type: "text", nullable: true),
                    DefenderSelectedArmyIds = table.Column<string>(type: "text", nullable: true),
                    AttackerLineupConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    DefenderLineupConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclaredAttacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    CurrentPhase = table.Column<string>(type: "text", nullable: false),
                    WinCondition = table.Column<string>(type: "text", nullable: false),
                    MaxPlayers = table.Column<int>(type: "integer", nullable: false),
                    LobbyCode = table.Column<string>(type: "text", nullable: false),
                    MapWidth = table.Column<int>(type: "integer", nullable: false),
                    MapHeight = table.Column<int>(type: "integer", nullable: false),
                    HostUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentTurnKingdomId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaxRounds = table.Column<int>(type: "integer", nullable: false),
                    BaseActionPoints = table.Column<int>(type: "integer", nullable: false),
                    HealPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    SpinCostGold = table.Column<int>(type: "integer", nullable: false),
                    SlotOutcomeWeights = table.Column<string>(type: "jsonb", nullable: false),
                    TurnTimeLimit = table.Column<int>(type: "integer", nullable: true),
                    TurnDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemainingActionPoints = table.Column<int>(type: "integer", nullable: true),
                    WinnerKingdomId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_AspNetUsers_HostUserId",
                        column: x => x.HostUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kingdoms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TurnOrder = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FactionTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefeatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kingdoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kingdoms_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kingdoms_FactionTypes_FactionTypeId",
                        column: x => x.FactionTypeId,
                        principalTable: "FactionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kingdoms_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KingdomResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KingdomId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KingdomResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KingdomResources_Kingdoms_KingdomId",
                        column: x => x.KingdomId,
                        principalTable: "Kingdoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoordQ = table.Column<int>(type: "integer", nullable: false),
                    CoordR = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    TerrainTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    KingdomId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsCastle = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tiles_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tiles_Kingdoms_KingdomId",
                        column: x => x.KingdomId,
                        principalTable: "Kingdoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tiles_TerrainTypes_TerrainTypeId",
                        column: x => x.TerrainTypeId,
                        principalTable: "TerrainTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TurnLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    KingdomId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TurnLogs_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TurnLogs_Kingdoms_KingdomId",
                        column: x => x.KingdomId,
                        principalTable: "Kingdoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Armies_ArmyTypeId",
                table: "Armies",
                column: "ArmyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Armies_BuildingId",
                table: "Armies",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Armies_KingdomId",
                table: "Armies",
                column: "KingdomId");

            migrationBuilder.CreateIndex(
                name: "IX_Armies_TileId",
                table: "Armies",
                column: "TileId");

            migrationBuilder.CreateIndex(
                name: "IX_ArmyTypes_RequiredBuildingTypeId",
                table: "ArmyTypes",
                column: "RequiredBuildingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BattleRounds_BattleId_RoundNumber",
                table: "BattleRounds",
                columns: new[] { "BattleId", "RoundNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Battles_AttackerKingdomId",
                table: "Battles",
                column: "AttackerKingdomId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_AttackerTileId",
                table: "Battles",
                column: "AttackerTileId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_DefenderKingdomId",
                table: "Battles",
                column: "DefenderKingdomId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_DefenderTileId",
                table: "Battles",
                column: "DefenderTileId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_GameId",
                table: "Battles",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_TileCapturedFromKingdomId",
                table: "Battles",
                column: "TileCapturedFromKingdomId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_TileCapturedId",
                table: "Battles",
                column: "TileCapturedId");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_BuildingTypeId",
                table: "Buildings",
                column: "BuildingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_KingdomId",
                table: "Buildings",
                column: "KingdomId");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_TileId",
                table: "Buildings",
                column: "TileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildingTypes_UnlockedByBuildingTypeId",
                table: "BuildingTypes",
                column: "UnlockedByBuildingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclaredAttacks_AttackerKingdomId",
                table: "DeclaredAttacks",
                column: "AttackerKingdomId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclaredAttacks_DefenderKingdomId",
                table: "DeclaredAttacks",
                column: "DefenderKingdomId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclaredAttacks_GameId",
                table: "DeclaredAttacks",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclaredAttacks_RiskedTileId",
                table: "DeclaredAttacks",
                column: "RiskedTileId");

            migrationBuilder.CreateIndex(
                name: "IX_DeclaredAttacks_TargetTileId",
                table: "DeclaredAttacks",
                column: "TargetTileId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_CurrentTurnKingdomId",
                table: "Games",
                column: "CurrentTurnKingdomId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_HostUserId",
                table: "Games",
                column: "HostUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_LobbyCode",
                table: "Games",
                column: "LobbyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_WinnerKingdomId",
                table: "Games",
                column: "WinnerKingdomId");

            migrationBuilder.CreateIndex(
                name: "IX_KingdomResources_KingdomId_ResourceType",
                table: "KingdomResources",
                columns: new[] { "KingdomId", "ResourceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kingdoms_AppUserId",
                table: "Kingdoms",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Kingdoms_FactionTypeId",
                table: "Kingdoms",
                column: "FactionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Kingdoms_GameId",
                table: "Kingdoms",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tiles_GameId_CoordQ_CoordR",
                table: "Tiles",
                columns: new[] { "GameId", "CoordQ", "CoordR" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tiles_KingdomId",
                table: "Tiles",
                column: "KingdomId");

            migrationBuilder.CreateIndex(
                name: "IX_Tiles_TerrainTypeId",
                table: "Tiles",
                column: "TerrainTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnLogs_GameId_KingdomId",
                table: "TurnLogs",
                columns: new[] { "GameId", "KingdomId" });

            migrationBuilder.CreateIndex(
                name: "IX_TurnLogs_GameId_RoundNumber",
                table: "TurnLogs",
                columns: new[] { "GameId", "RoundNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_TurnLogs_KingdomId",
                table: "TurnLogs",
                column: "KingdomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Armies_Buildings_BuildingId",
                table: "Armies",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Armies_Kingdoms_KingdomId",
                table: "Armies",
                column: "KingdomId",
                principalTable: "Kingdoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Armies_Tiles_TileId",
                table: "Armies",
                column: "TileId",
                principalTable: "Tiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BattleRounds_Battles_BattleId",
                table: "BattleRounds",
                column: "BattleId",
                principalTable: "Battles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Battles_Games_GameId",
                table: "Battles",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Battles_Kingdoms_AttackerKingdomId",
                table: "Battles",
                column: "AttackerKingdomId",
                principalTable: "Kingdoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Battles_Kingdoms_DefenderKingdomId",
                table: "Battles",
                column: "DefenderKingdomId",
                principalTable: "Kingdoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Battles_Kingdoms_TileCapturedFromKingdomId",
                table: "Battles",
                column: "TileCapturedFromKingdomId",
                principalTable: "Kingdoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Battles_Tiles_AttackerTileId",
                table: "Battles",
                column: "AttackerTileId",
                principalTable: "Tiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Battles_Tiles_DefenderTileId",
                table: "Battles",
                column: "DefenderTileId",
                principalTable: "Tiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Battles_Tiles_TileCapturedId",
                table: "Battles",
                column: "TileCapturedId",
                principalTable: "Tiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Buildings_Kingdoms_KingdomId",
                table: "Buildings",
                column: "KingdomId",
                principalTable: "Kingdoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Buildings_Tiles_TileId",
                table: "Buildings",
                column: "TileId",
                principalTable: "Tiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeclaredAttacks_Games_GameId",
                table: "DeclaredAttacks",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeclaredAttacks_Kingdoms_AttackerKingdomId",
                table: "DeclaredAttacks",
                column: "AttackerKingdomId",
                principalTable: "Kingdoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeclaredAttacks_Kingdoms_DefenderKingdomId",
                table: "DeclaredAttacks",
                column: "DefenderKingdomId",
                principalTable: "Kingdoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeclaredAttacks_Tiles_RiskedTileId",
                table: "DeclaredAttacks",
                column: "RiskedTileId",
                principalTable: "Tiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DeclaredAttacks_Tiles_TargetTileId",
                table: "DeclaredAttacks",
                column: "TargetTileId",
                principalTable: "Tiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Kingdoms_CurrentTurnKingdomId",
                table: "Games",
                column: "CurrentTurnKingdomId",
                principalTable: "Kingdoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Kingdoms_WinnerKingdomId",
                table: "Games",
                column: "WinnerKingdomId",
                principalTable: "Kingdoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Kingdoms_CurrentTurnKingdomId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_Kingdoms_WinnerKingdomId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "Armies");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BattleRounds");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "DeclaredAttacks");

            migrationBuilder.DropTable(
                name: "GameEvents");

            migrationBuilder.DropTable(
                name: "KingdomResources");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "TurnLogs");

            migrationBuilder.DropTable(
                name: "ArmyTypes");

            migrationBuilder.DropTable(
                name: "Buildings");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Battles");

            migrationBuilder.DropTable(
                name: "BuildingTypes");

            migrationBuilder.DropTable(
                name: "Tiles");

            migrationBuilder.DropTable(
                name: "TerrainTypes");

            migrationBuilder.DropTable(
                name: "Kingdoms");

            migrationBuilder.DropTable(
                name: "FactionTypes");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
