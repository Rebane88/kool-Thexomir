using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentTurnKingdomIdToGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Buildings_TileId",
                table: "Buildings");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentTurnKingdomId",
                table: "Games",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_CurrentTurnKingdomId",
                table: "Games",
                column: "CurrentTurnKingdomId");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_TileId",
                table: "Buildings",
                column: "TileId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Kingdoms_CurrentTurnKingdomId",
                table: "Games",
                column: "CurrentTurnKingdomId",
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

            migrationBuilder.DropIndex(
                name: "IX_Games_CurrentTurnKingdomId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Buildings_TileId",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "CurrentTurnKingdomId",
                table: "Games");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_TileId",
                table: "Buildings",
                column: "TileId");
        }
    }
}
