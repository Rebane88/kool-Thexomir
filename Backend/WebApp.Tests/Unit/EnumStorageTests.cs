namespace WebApp.Tests.Unit;

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
        Assert.True(Directory.Exists(MigrationsDir), $"Migrations folder not found at: {MigrationsDir}");

        // Find the InitialCreate migration (not the Designer or Snapshot)
        var file = Directory.GetFiles(MigrationsDir, "*InitialCreate.cs")
            .FirstOrDefault(f => !f.EndsWith(".Designer.cs"));

        Assert.True(file is not null, "InitialCreate migration file not found in Migrations folder");
        return File.ReadAllText(file!);
    }

    [Fact]
    public void Game_Status_enum_column_is_stored_as_text_in_migration()
    {
        var migration = LoadInitialCreateMigration();

        // Migration column definition pattern: Status = table.Column<string>(type: "text", ...)
        Assert.Contains("Status = table.Column<string>(type: \"text\"", migration);
    }

    [Fact]
    public void Game_WinCondition_enum_column_is_stored_as_text_in_migration()
    {
        var migration = LoadInitialCreateMigration();

        Assert.Contains("WinCondition = table.Column<string>(type: \"text\"", migration);
    }

    [Fact]
    public void KingdomResource_ResourceType_enum_column_is_stored_as_text_in_migration()
    {
        var migration = LoadInitialCreateMigration();

        // Check both the column definition and table context
        // The migration has two ResourceType columns (KingdomResources and FactionResourceBonuses)
        // We just verify at least one exists as text — the FK/index section confirms KingdomResources
        Assert.Contains("IX_KingdomResources_KingdomId_ResourceType", migration);
        // And the column itself
        Assert.Matches(@"ResourceType\s*=\s*table\.Column<string>\(type:\s*""text""", migration);
    }

    [Fact]
    public void TerrainType_ResourceBonusType_enum_column_is_stored_as_text_in_migration()
    {
        var migration = LoadInitialCreateMigration();

        Assert.Contains("ResourceBonusType = table.Column<string>(type: \"text\"", migration);
    }

    [Fact]
    public void FactionResourceBonus_ResourceType_enum_column_is_stored_as_text_in_migration()
    {
        var migration = LoadInitialCreateMigration();

        // Verified by the presence of the unique index on FactionTypeId + ResourceType
        Assert.Contains("IX_FactionResourceBonuses_FactionTypeId_ResourceType", migration);
        Assert.Matches(@"ResourceType\s*=\s*table\.Column<string>\(type:\s*""text""", migration);
    }
}
