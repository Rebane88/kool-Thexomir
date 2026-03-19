using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeclaredAttack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RemainingActionPoints",
                table: "Games",
                type: "integer",
                nullable: true);

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
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeclaredAttacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeclaredAttacks_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeclaredAttacks_Kingdoms_AttackerKingdomId",
                        column: x => x.AttackerKingdomId,
                        principalTable: "Kingdoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeclaredAttacks_Kingdoms_DefenderKingdomId",
                        column: x => x.DefenderKingdomId,
                        principalTable: "Kingdoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeclaredAttacks_Tiles_RiskedTileId",
                        column: x => x.RiskedTileId,
                        principalTable: "Tiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeclaredAttacks_Tiles_TargetTileId",
                        column: x => x.TargetTileId,
                        principalTable: "Tiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeclaredAttacks");

            migrationBuilder.DropColumn(
                name: "RemainingActionPoints",
                table: "Games");
        }
    }
}
