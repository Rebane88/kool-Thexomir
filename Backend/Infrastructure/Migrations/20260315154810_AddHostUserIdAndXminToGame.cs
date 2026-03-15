using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHostUserIdAndXminToGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HostUserId",
                table: "Games",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Games",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_Games_HostUserId",
                table: "Games",
                column: "HostUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_AspNetUsers_HostUserId",
                table: "Games",
                column: "HostUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_AspNetUsers_HostUserId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_HostUserId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "HostUserId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Games");
        }
    }
}
