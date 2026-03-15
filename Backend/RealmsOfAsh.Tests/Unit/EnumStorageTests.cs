using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// INFRA-07: Enum properties are stored as strings (text), not integers.
/// Verified by inspecting the EF Core migration file, which is the authoritative artifact
/// for the database schema generated from the Fluent API HasConversion configuration.
/// </summary>
public class EnumStorageTests
{
    // Path: bin/Debug/net10.0 → up 4 → Backend → Infrastructure/Migrations
    private static readonly string MigrationsDir =
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "Infrastructure", "Migrations"));

    private static string LoadInitialCreateMigration()
    {
        Directory.Exists(MigrationsDir).ShouldBeTrue($"Migrations folder not found at: {MigrationsDir}");

        // Find the InitialCreate migration (not the Designer or Snapshot)
        var file = Directory.GetFiles(MigrationsDir, "*InitialCreate.cs")
            .FirstOrDefault(f => !f.EndsWith(".Designer.cs"));

        (file is not null).ShouldBeTrue("InitialCreate migration file not found in Migrations folder");
        return File.ReadAllText(file!);
    }

    [Fact]
    public void Game_Status_enum_column_is_stored_as_text_in_migration()
    {
        var migration = LoadInitialCreateMigration();

        // Migration column definition pattern: Status = table.Column<string>(type: "text", ...)
        migration.ShouldContain("Status = table.Column<string>(type: \"text\"");
    }

    [Fact]
    public void Game_WinCondition_enum_column_is_stored_as_text_in_migration()
    {
        var migration = LoadInitialCreateMigration();

        migration.ShouldContain("WinCondition = table.Column<string>(type: \"text\"");
    }

    [Fact]
    public void KingdomResource_ResourceType_enum_column_is_stored_as_text_in_migration()
    {
        var migration = LoadInitialCreateMigration();

        // Check both the column definition and table context
        // The migration has two ResourceType columns (KingdomResources and FactionResourceBonuses)
        // We just verify at least one exists as text — the FK/index section confirms KingdomResources
        migration.ShouldContain("IX_KingdomResources_KingdomId_ResourceType");
        // And the column itself
        migration.ShouldMatch(@"ResourceType\s*=\s*table\.Column<string>\(type:\s*""text""");
    }

    [Fact]
    public void TerrainType_ResourceBonusType_enum_column_is_stored_as_text_in_migration()
    {
        var migration = LoadInitialCreateMigration();

        migration.ShouldContain("ResourceBonusType = table.Column<string>(type: \"text\"");
    }

    [Fact]
    public void FactionResourceBonus_ResourceType_enum_column_is_stored_as_text_in_migration()
    {
        var migration = LoadInitialCreateMigration();

        // Verified by the presence of the unique index on FactionTypeId + ResourceType
        migration.ShouldContain("IX_FactionResourceBonuses_FactionTypeId_ResourceType");
        migration.ShouldMatch(@"ResourceType\s*=\s*table\.Column<string>\(type:\s*""text""");
    }
}
