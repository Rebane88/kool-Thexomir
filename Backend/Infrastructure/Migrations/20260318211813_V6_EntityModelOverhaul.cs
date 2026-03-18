using System;
using Base;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V6_EntityModelOverhaul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Battles_Armies_AttackerArmyId",
                table: "Battles");

            migrationBuilder.DropForeignKey(
                name: "FK_Battles_Armies_DefenderArmyId",
                table: "Battles");

            migrationBuilder.DropForeignKey(
                name: "FK_Battles_Kingdoms_WinnerId",
                table: "Battles");

            migrationBuilder.DropForeignKey(
                name: "FK_Battles_Tiles_TileId",
                table: "Battles");

            migrationBuilder.DropForeignKey(
                name: "FK_BuildingTypes_BuildingTypes_PrerequisiteBuildingTypeId",
                table: "BuildingTypes");

            migrationBuilder.DropTable(
                name: "BuildingUnitTypes");

            migrationBuilder.DropTable(
                name: "FactionResourceBonuses");

            migrationBuilder.DropTable(
                name: "FactionUnitBonuses");

            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "UnitTypeMatchups");

            migrationBuilder.DropTable(
                name: "UnitTypes");

            migrationBuilder.DropIndex(
                name: "IX_TurnLogs_GameId",
                table: "TurnLogs");

            migrationBuilder.DropIndex(
                name: "IX_Battles_WinnerId",
                table: "Battles");

            migrationBuilder.DropColumn(
                name: "MovementCost",
                table: "TerrainTypes");

            migrationBuilder.DropColumn(
                name: "IsEliminated",
                table: "Kingdoms");

            migrationBuilder.DropColumn(
                name: "StartingFood",
                table: "FactionTypes");

            migrationBuilder.DropColumn(
                name: "StartingGold",
                table: "FactionTypes");

            migrationBuilder.DropColumn(
                name: "StartingMana",
                table: "FactionTypes");

            migrationBuilder.DropColumn(
                name: "HasTrainedThisTurn",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "WinnerId",
                table: "Battles");

            migrationBuilder.DropColumn(
                name: "HasAttackedThisTurn",
                table: "Armies");

            migrationBuilder.RenameColumn(
                name: "TurnNumber",
                table: "TurnLogs",
                newName: "RoundNumber");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "TurnLogs",
                newName: "EventType");

            migrationBuilder.RenameColumn(
                name: "IsCapital",
                table: "Tiles",
                newName: "IsCastle");

            migrationBuilder.RenameColumn(
                name: "DefenseBonus",
                table: "TerrainTypes",
                newName: "ResourceMultiplier");

            migrationBuilder.RenameColumn(
                name: "TurnNumber",
                table: "Games",
                newName: "SpinCostGold");

            migrationBuilder.RenameColumn(
                name: "MaxTurnCount",
                table: "Games",
                newName: "TurnTimeLimit");

            migrationBuilder.RenameColumn(
                name: "StartingWood",
                table: "FactionTypes",
                newName: "StartingBonusAmount");

            migrationBuilder.RenameColumn(
                name: "StartingStone",
                table: "FactionTypes",
                newName: "ActionPointModifier");

            migrationBuilder.RenameColumn(
                name: "WoodYield",
                table: "BuildingTypes",
                newName: "CostWood");

            migrationBuilder.RenameColumn(
                name: "WoodCost",
                table: "BuildingTypes",
                newName: "CostStone");

            migrationBuilder.RenameColumn(
                name: "StoneYield",
                table: "BuildingTypes",
                newName: "CostMana");

            migrationBuilder.RenameColumn(
                name: "StoneCost",
                table: "BuildingTypes",
                newName: "CostGold");

            migrationBuilder.RenameColumn(
                name: "PrerequisiteBuildingTypeId",
                table: "BuildingTypes",
                newName: "UnlockedByBuildingTypeId");

            migrationBuilder.RenameColumn(
                name: "ManaYield",
                table: "BuildingTypes",
                newName: "CostFood");

            migrationBuilder.RenameColumn(
                name: "ManaCost",
                table: "BuildingTypes",
                newName: "BaseYieldWood");

            migrationBuilder.RenameColumn(
                name: "GoldYield",
                table: "BuildingTypes",
                newName: "BaseYieldStone");

            migrationBuilder.RenameColumn(
                name: "GoldCost",
                table: "BuildingTypes",
                newName: "BaseYieldMana");

            migrationBuilder.RenameColumn(
                name: "FoodYield",
                table: "BuildingTypes",
                newName: "BaseYieldGold");

            migrationBuilder.RenameIndex(
                name: "IX_BuildingTypes_PrerequisiteBuildingTypeId",
                table: "BuildingTypes",
                newName: "IX_BuildingTypes_UnlockedByBuildingTypeId");

            migrationBuilder.RenameColumn(
                name: "TurnNumber",
                table: "Battles",
                newName: "RoundNumber");

            migrationBuilder.RenameColumn(
                name: "TileId",
                table: "Battles",
                newName: "TileCapturedId");

            migrationBuilder.RenameColumn(
                name: "DefenderArmyId",
                table: "Battles",
                newName: "TileCapturedFromKingdomId");

            migrationBuilder.RenameColumn(
                name: "AttackerArmyId",
                table: "Battles",
                newName: "DefenderTileId");

            migrationBuilder.RenameIndex(
                name: "IX_Battles_TileId",
                table: "Battles",
                newName: "IX_Battles_TileCapturedId");

            migrationBuilder.RenameIndex(
                name: "IX_Battles_DefenderArmyId",
                table: "Battles",
                newName: "IX_Battles_TileCapturedFromKingdomId");

            migrationBuilder.RenameIndex(
                name: "IX_Battles_AttackerArmyId",
                table: "Battles",
                newName: "IX_Battles_DefenderTileId");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TurnLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "TurnLogs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OccurredAt",
                table: "TurnLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "IconUrl",
                table: "TerrainTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MapColor",
                table: "TerrainTypes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "FactionTypeId",
                table: "Kingdoms",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Kingdoms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DefeatedAt",
                table: "Kingdoms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedAt",
                table: "Kingdoms",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Kingdoms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TurnOrder",
                table: "Kingdoms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BaseActionPoints",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CurrentPhase",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "Games",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HealPercent",
                table: "Games",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MaxRounds",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RoundNumber",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SlotOutcomeWeights",
                table: "Games",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "Games",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TurnDeadline",
                table: "Games",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AttackModifier",
                table: "FactionTypes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ChipDamageModifier",
                table: "FactionTypes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HPModifier",
                table: "FactionTypes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HealRateModifier",
                table: "FactionTypes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "IconUrl",
                table: "FactionTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InitiativeModifier",
                table: "FactionTypes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Lore",
                table: "FactionTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResourceProductionModifier",
                table: "FactionTypes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "StartingBonusResource",
                table: "FactionTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TrainingCostModifier",
                table: "FactionTypes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ArmyCapacity",
                table: "BuildingTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BaseYieldFood",
                table: "BuildingTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "BuiltAt",
                table: "Buildings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "BuiltOnRound",
                table: "Buildings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "KingdomId",
                table: "Buildings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "AttackerKingdomId",
                table: "Battles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "AttackerTileId",
                table: "Battles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DefenderKingdomId",
                table: "Battles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "OccurredAt",
                table: "Battles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Outcome",
                table: "Battles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "TileId",
                table: "Armies",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ArmyTypeId",
                table: "Armies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "BuildingId",
                table: "Armies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "CreatedOnRound",
                table: "Armies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentHP",
                table: "Armies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxHP",
                table: "Armies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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
                    Description = table.Column<string>(type: "text", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_BattleRounds_Armies_ArmyDestroyedId",
                        column: x => x.ArmyDestroyedId,
                        principalTable: "Armies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BattleRounds_Armies_AttackerArmyId",
                        column: x => x.AttackerArmyId,
                        principalTable: "Armies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BattleRounds_Armies_DefenderArmyId",
                        column: x => x.DefenderArmyId,
                        principalTable: "Armies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BattleRounds_Battles_BattleId",
                        column: x => x.BattleId,
                        principalTable: "Battles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TurnLogs_GameId_KingdomId",
                table: "TurnLogs",
                columns: new[] { "GameId", "KingdomId" });

            migrationBuilder.CreateIndex(
                name: "IX_TurnLogs_GameId_RoundNumber",
                table: "TurnLogs",
                columns: new[] { "GameId", "RoundNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_KingdomId",
                table: "Buildings",
                column: "KingdomId");

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
                name: "IX_Armies_ArmyTypeId",
                table: "Armies",
                column: "ArmyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Armies_BuildingId",
                table: "Armies",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_ArmyTypes_RequiredBuildingTypeId",
                table: "ArmyTypes",
                column: "RequiredBuildingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BattleRounds_ArmyDestroyedId",
                table: "BattleRounds",
                column: "ArmyDestroyedId");

            migrationBuilder.CreateIndex(
                name: "IX_BattleRounds_AttackerArmyId",
                table: "BattleRounds",
                column: "AttackerArmyId");

            migrationBuilder.CreateIndex(
                name: "IX_BattleRounds_BattleId_RoundNumber",
                table: "BattleRounds",
                columns: new[] { "BattleId", "RoundNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BattleRounds_DefenderArmyId",
                table: "BattleRounds",
                column: "DefenderArmyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Armies_ArmyTypes_ArmyTypeId",
                table: "Armies",
                column: "ArmyTypeId",
                principalTable: "ArmyTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Armies_Buildings_BuildingId",
                table: "Armies",
                column: "BuildingId",
                principalTable: "Buildings",
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
                name: "FK_BuildingTypes_BuildingTypes_UnlockedByBuildingTypeId",
                table: "BuildingTypes",
                column: "UnlockedByBuildingTypeId",
                principalTable: "BuildingTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Armies_ArmyTypes_ArmyTypeId",
                table: "Armies");

            migrationBuilder.DropForeignKey(
                name: "FK_Armies_Buildings_BuildingId",
                table: "Armies");

            migrationBuilder.DropForeignKey(
                name: "FK_Battles_Kingdoms_AttackerKingdomId",
                table: "Battles");

            migrationBuilder.DropForeignKey(
                name: "FK_Battles_Kingdoms_DefenderKingdomId",
                table: "Battles");

            migrationBuilder.DropForeignKey(
                name: "FK_Battles_Kingdoms_TileCapturedFromKingdomId",
                table: "Battles");

            migrationBuilder.DropForeignKey(
                name: "FK_Battles_Tiles_AttackerTileId",
                table: "Battles");

            migrationBuilder.DropForeignKey(
                name: "FK_Battles_Tiles_DefenderTileId",
                table: "Battles");

            migrationBuilder.DropForeignKey(
                name: "FK_Battles_Tiles_TileCapturedId",
                table: "Battles");

            migrationBuilder.DropForeignKey(
                name: "FK_Buildings_Kingdoms_KingdomId",
                table: "Buildings");

            migrationBuilder.DropForeignKey(
                name: "FK_BuildingTypes_BuildingTypes_UnlockedByBuildingTypeId",
                table: "BuildingTypes");

            migrationBuilder.DropTable(
                name: "ArmyTypes");

            migrationBuilder.DropTable(
                name: "BattleRounds");

            migrationBuilder.DropIndex(
                name: "IX_TurnLogs_GameId_KingdomId",
                table: "TurnLogs");

            migrationBuilder.DropIndex(
                name: "IX_TurnLogs_GameId_RoundNumber",
                table: "TurnLogs");

            migrationBuilder.DropIndex(
                name: "IX_Buildings_KingdomId",
                table: "Buildings");

            migrationBuilder.DropIndex(
                name: "IX_Battles_AttackerKingdomId",
                table: "Battles");

            migrationBuilder.DropIndex(
                name: "IX_Battles_AttackerTileId",
                table: "Battles");

            migrationBuilder.DropIndex(
                name: "IX_Battles_DefenderKingdomId",
                table: "Battles");

            migrationBuilder.DropIndex(
                name: "IX_Armies_ArmyTypeId",
                table: "Armies");

            migrationBuilder.DropIndex(
                name: "IX_Armies_BuildingId",
                table: "Armies");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TurnLogs");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "TurnLogs");

            migrationBuilder.DropColumn(
                name: "OccurredAt",
                table: "TurnLogs");

            migrationBuilder.DropColumn(
                name: "IconUrl",
                table: "TerrainTypes");

            migrationBuilder.DropColumn(
                name: "MapColor",
                table: "TerrainTypes");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Kingdoms");

            migrationBuilder.DropColumn(
                name: "DefeatedAt",
                table: "Kingdoms");

            migrationBuilder.DropColumn(
                name: "JoinedAt",
                table: "Kingdoms");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Kingdoms");

            migrationBuilder.DropColumn(
                name: "TurnOrder",
                table: "Kingdoms");

            migrationBuilder.DropColumn(
                name: "BaseActionPoints",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "CurrentPhase",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "HealPercent",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "MaxRounds",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "RoundNumber",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "SlotOutcomeWeights",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "TurnDeadline",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "AttackModifier",
                table: "FactionTypes");

            migrationBuilder.DropColumn(
                name: "ChipDamageModifier",
                table: "FactionTypes");

            migrationBuilder.DropColumn(
                name: "HPModifier",
                table: "FactionTypes");

            migrationBuilder.DropColumn(
                name: "HealRateModifier",
                table: "FactionTypes");

            migrationBuilder.DropColumn(
                name: "IconUrl",
                table: "FactionTypes");

            migrationBuilder.DropColumn(
                name: "InitiativeModifier",
                table: "FactionTypes");

            migrationBuilder.DropColumn(
                name: "Lore",
                table: "FactionTypes");

            migrationBuilder.DropColumn(
                name: "ResourceProductionModifier",
                table: "FactionTypes");

            migrationBuilder.DropColumn(
                name: "StartingBonusResource",
                table: "FactionTypes");

            migrationBuilder.DropColumn(
                name: "TrainingCostModifier",
                table: "FactionTypes");

            migrationBuilder.DropColumn(
                name: "ArmyCapacity",
                table: "BuildingTypes");

            migrationBuilder.DropColumn(
                name: "BaseYieldFood",
                table: "BuildingTypes");

            migrationBuilder.DropColumn(
                name: "BuiltAt",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "BuiltOnRound",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "KingdomId",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "AttackerKingdomId",
                table: "Battles");

            migrationBuilder.DropColumn(
                name: "AttackerTileId",
                table: "Battles");

            migrationBuilder.DropColumn(
                name: "DefenderKingdomId",
                table: "Battles");

            migrationBuilder.DropColumn(
                name: "OccurredAt",
                table: "Battles");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "Battles");

            migrationBuilder.DropColumn(
                name: "ArmyTypeId",
                table: "Armies");

            migrationBuilder.DropColumn(
                name: "BuildingId",
                table: "Armies");

            migrationBuilder.DropColumn(
                name: "CreatedOnRound",
                table: "Armies");

            migrationBuilder.DropColumn(
                name: "CurrentHP",
                table: "Armies");

            migrationBuilder.DropColumn(
                name: "MaxHP",
                table: "Armies");

            migrationBuilder.RenameColumn(
                name: "RoundNumber",
                table: "TurnLogs",
                newName: "TurnNumber");

            migrationBuilder.RenameColumn(
                name: "EventType",
                table: "TurnLogs",
                newName: "Action");

            migrationBuilder.RenameColumn(
                name: "IsCastle",
                table: "Tiles",
                newName: "IsCapital");

            migrationBuilder.RenameColumn(
                name: "ResourceMultiplier",
                table: "TerrainTypes",
                newName: "DefenseBonus");

            migrationBuilder.RenameColumn(
                name: "TurnTimeLimit",
                table: "Games",
                newName: "MaxTurnCount");

            migrationBuilder.RenameColumn(
                name: "SpinCostGold",
                table: "Games",
                newName: "TurnNumber");

            migrationBuilder.RenameColumn(
                name: "StartingBonusAmount",
                table: "FactionTypes",
                newName: "StartingWood");

            migrationBuilder.RenameColumn(
                name: "ActionPointModifier",
                table: "FactionTypes",
                newName: "StartingStone");

            migrationBuilder.RenameColumn(
                name: "UnlockedByBuildingTypeId",
                table: "BuildingTypes",
                newName: "PrerequisiteBuildingTypeId");

            migrationBuilder.RenameColumn(
                name: "CostWood",
                table: "BuildingTypes",
                newName: "WoodYield");

            migrationBuilder.RenameColumn(
                name: "CostStone",
                table: "BuildingTypes",
                newName: "WoodCost");

            migrationBuilder.RenameColumn(
                name: "CostMana",
                table: "BuildingTypes",
                newName: "StoneYield");

            migrationBuilder.RenameColumn(
                name: "CostGold",
                table: "BuildingTypes",
                newName: "StoneCost");

            migrationBuilder.RenameColumn(
                name: "CostFood",
                table: "BuildingTypes",
                newName: "ManaYield");

            migrationBuilder.RenameColumn(
                name: "BaseYieldWood",
                table: "BuildingTypes",
                newName: "ManaCost");

            migrationBuilder.RenameColumn(
                name: "BaseYieldStone",
                table: "BuildingTypes",
                newName: "GoldYield");

            migrationBuilder.RenameColumn(
                name: "BaseYieldMana",
                table: "BuildingTypes",
                newName: "GoldCost");

            migrationBuilder.RenameColumn(
                name: "BaseYieldGold",
                table: "BuildingTypes",
                newName: "FoodYield");

            migrationBuilder.RenameIndex(
                name: "IX_BuildingTypes_UnlockedByBuildingTypeId",
                table: "BuildingTypes",
                newName: "IX_BuildingTypes_PrerequisiteBuildingTypeId");

            migrationBuilder.RenameColumn(
                name: "TileCapturedId",
                table: "Battles",
                newName: "TileId");

            migrationBuilder.RenameColumn(
                name: "TileCapturedFromKingdomId",
                table: "Battles",
                newName: "DefenderArmyId");

            migrationBuilder.RenameColumn(
                name: "RoundNumber",
                table: "Battles",
                newName: "TurnNumber");

            migrationBuilder.RenameColumn(
                name: "DefenderTileId",
                table: "Battles",
                newName: "AttackerArmyId");

            migrationBuilder.RenameIndex(
                name: "IX_Battles_TileCapturedId",
                table: "Battles",
                newName: "IX_Battles_TileId");

            migrationBuilder.RenameIndex(
                name: "IX_Battles_TileCapturedFromKingdomId",
                table: "Battles",
                newName: "IX_Battles_DefenderArmyId");

            migrationBuilder.RenameIndex(
                name: "IX_Battles_DefenderTileId",
                table: "Battles",
                newName: "IX_Battles_AttackerArmyId");

            migrationBuilder.AddColumn<int>(
                name: "MovementCost",
                table: "TerrainTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "FactionTypeId",
                table: "Kingdoms",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "IsEliminated",
                table: "Kingdoms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StartingFood",
                table: "FactionTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StartingGold",
                table: "FactionTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StartingMana",
                table: "FactionTypes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasTrainedThisTurn",
                table: "Buildings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "WinnerId",
                table: "Battles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TileId",
                table: "Armies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAttackedThisTurn",
                table: "Armies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FactionResourceBonuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FactionTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Multiplier = table.Column<decimal>(type: "numeric", nullable: false),
                    ResourceType = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionResourceBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactionResourceBonuses_FactionTypes_FactionTypeId",
                        column: x => x.FactionTypeId,
                        principalTable: "FactionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseStrength = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    FoodCost = table.Column<int>(type: "integer", nullable: false),
                    GoldCost = table.Column<int>(type: "integer", nullable: false),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    ManaCost = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<LangStr>(type: "jsonb", nullable: false),
                    StoneCost = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Upkeep = table.Column<int>(type: "integer", nullable: false),
                    WoodCost = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BuildingUnitTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingUnitTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuildingUnitTypes_BuildingTypes_BuildingTypeId",
                        column: x => x.BuildingTypeId,
                        principalTable: "BuildingTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuildingUnitTypes_UnitTypes_UnitTypeId",
                        column: x => x.UnitTypeId,
                        principalTable: "UnitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FactionUnitBonuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FactionTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Multiplier = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionUnitBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactionUnitBonuses_FactionTypes_FactionTypeId",
                        column: x => x.FactionTypeId,
                        principalTable: "FactionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactionUnitBonuses_UnitTypes_UnitTypeId",
                        column: x => x.UnitTypeId,
                        principalTable: "UnitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArmyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Units_Armies_ArmyId",
                        column: x => x.ArmyId,
                        principalTable: "Armies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Units_UnitTypes_UnitTypeId",
                        column: x => x.UnitTypeId,
                        principalTable: "UnitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitTypeMatchups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttackerTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenderTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Multiplier = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitTypeMatchups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitTypeMatchups_UnitTypes_AttackerTypeId",
                        column: x => x.AttackerTypeId,
                        principalTable: "UnitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitTypeMatchups_UnitTypes_DefenderTypeId",
                        column: x => x.DefenderTypeId,
                        principalTable: "UnitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TurnLogs_GameId",
                table: "TurnLogs",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_WinnerId",
                table: "Battles",
                column: "WinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingUnitTypes_BuildingTypeId_UnitTypeId",
                table: "BuildingUnitTypes",
                columns: new[] { "BuildingTypeId", "UnitTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildingUnitTypes_UnitTypeId",
                table: "BuildingUnitTypes",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionResourceBonuses_FactionTypeId_ResourceType",
                table: "FactionResourceBonuses",
                columns: new[] { "FactionTypeId", "ResourceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactionUnitBonuses_FactionTypeId_UnitTypeId",
                table: "FactionUnitBonuses",
                columns: new[] { "FactionTypeId", "UnitTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FactionUnitBonuses_UnitTypeId",
                table: "FactionUnitBonuses",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_ArmyId",
                table: "Units",
                column: "ArmyId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_UnitTypeId",
                table: "Units",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitTypeMatchups_AttackerTypeId_DefenderTypeId",
                table: "UnitTypeMatchups",
                columns: new[] { "AttackerTypeId", "DefenderTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitTypeMatchups_DefenderTypeId",
                table: "UnitTypeMatchups",
                column: "DefenderTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Battles_Armies_AttackerArmyId",
                table: "Battles",
                column: "AttackerArmyId",
                principalTable: "Armies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Battles_Armies_DefenderArmyId",
                table: "Battles",
                column: "DefenderArmyId",
                principalTable: "Armies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Battles_Kingdoms_WinnerId",
                table: "Battles",
                column: "WinnerId",
                principalTable: "Kingdoms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Battles_Tiles_TileId",
                table: "Battles",
                column: "TileId",
                principalTable: "Tiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BuildingTypes_BuildingTypes_PrerequisiteBuildingTypeId",
                table: "BuildingTypes",
                column: "PrerequisiteBuildingTypeId",
                principalTable: "BuildingTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
