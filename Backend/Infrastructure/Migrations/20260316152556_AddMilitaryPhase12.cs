using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMilitaryPhase12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasTrainedThisTurn",
                table: "Buildings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasAttackedThisTurn",
                table: "Armies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.CreateIndex(
                name: "IX_BuildingUnitTypes_BuildingTypeId_UnitTypeId",
                table: "BuildingUnitTypes",
                columns: new[] { "BuildingTypeId", "UnitTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildingUnitTypes_UnitTypeId",
                table: "BuildingUnitTypes",
                column: "UnitTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuildingUnitTypes");

            migrationBuilder.DropColumn(
                name: "HasTrainedThisTurn",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "HasAttackedThisTurn",
                table: "Armies");
        }
    }
}
