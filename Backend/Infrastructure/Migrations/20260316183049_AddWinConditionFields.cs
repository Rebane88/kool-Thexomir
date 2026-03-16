using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWinConditionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCapital",
                table: "Tiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxTurnCount",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WinnerKingdomId",
                table: "Games",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_WinnerKingdomId",
                table: "Games",
                column: "WinnerKingdomId");

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
                name: "FK_Games_Kingdoms_WinnerKingdomId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_WinnerKingdomId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "IsCapital",
                table: "Tiles");

            migrationBuilder.DropColumn(
                name: "MaxTurnCount",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "WinnerKingdomId",
                table: "Games");
        }
    }
}
