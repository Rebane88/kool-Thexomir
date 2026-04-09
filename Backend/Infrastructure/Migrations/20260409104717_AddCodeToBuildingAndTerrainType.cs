using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeToBuildingAndTerrainType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "TerrainTypes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "BuildingTypes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            // Backfill codes by known seeded Ids so the unique index below can be created
            // on existing databases. The seeders also set Code for fresh databases; this
            // UPDATE path only matters for databases that already hold the seed rows.
            migrationBuilder.Sql(@"
                UPDATE ""TerrainTypes"" SET ""Code"" = 'plains'       WHERE ""Id"" = 'AAAAAAAA-0001-0000-0000-000000000001';
                UPDATE ""TerrainTypes"" SET ""Code"" = 'forest'       WHERE ""Id"" = 'AAAAAAAA-0001-0000-0000-000000000002';
                UPDATE ""TerrainTypes"" SET ""Code"" = 'mountain'     WHERE ""Id"" = 'AAAAAAAA-0001-0000-0000-000000000003';
                UPDATE ""TerrainTypes"" SET ""Code"" = 'desert'       WHERE ""Id"" = 'AAAAAAAA-0001-0000-0000-000000000004';
                UPDATE ""TerrainTypes"" SET ""Code"" = 'magic-grove'  WHERE ""Id"" = 'AAAAAAAA-0001-0000-0000-000000000005';

                UPDATE ""BuildingTypes"" SET ""Code"" = 'castle'          WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-000000000100';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'farm'            WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-000000000001';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'windmill'        WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-000000000002';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'granary'         WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-000000000003';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'lumber-camp'     WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-000000000004';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'sawmill'         WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-000000000005';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'timber-hall'     WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-000000000006';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'quarry'          WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-000000000007';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'mason'           WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-000000000008';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'stoneworks'      WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-000000000009';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'market'          WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-00000000000A';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'trading-post'    WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-00000000000B';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'bank'            WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-00000000000C';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'shrine'          WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-00000000000D';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'wizard-tower'    WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-00000000000E';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'arcane-sanctum'  WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-00000000000F';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'barracks'        WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-000000000010';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'stables'         WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-000000000011';
                UPDATE ""BuildingTypes"" SET ""Code"" = 'war-academy'     WHERE ""Id"" = 'BBBBBBBB-0001-0000-0000-000000000012';
            ");

            migrationBuilder.CreateIndex(
                name: "IX_TerrainTypes_Code",
                table: "TerrainTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildingTypes_Code",
                table: "BuildingTypes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TerrainTypes_Code",
                table: "TerrainTypes");

            migrationBuilder.DropIndex(
                name: "IX_BuildingTypes_Code",
                table: "BuildingTypes");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "TerrainTypes");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "BuildingTypes");
        }
    }
}
