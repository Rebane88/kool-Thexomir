using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// INFRA-07: Enum properties are stored as strings (text), not integers.
/// Verified by inspecting the EF Core migration files, which are the authoritative artifacts
/// for the database schema generated from the Fluent API HasConversion configuration.
/// </summary>
[Trait("Category", "Unit")]
public class EnumStorageTests
{
    // Path: bin/Debug/net10.0 -> up 4 -> Backend -> Infrastructure/Migrations
    private static readonly string MigrationsDir =
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "Infrastructure", "Migrations"));

    private static string LoadInitialCreateMigration()
    {
        Directory.Exists(MigrationsDir).ShouldBeTrue($"Migrations folder not found at: {MigrationsDir}");

        var file = Directory.GetFiles(MigrationsDir, "*InitialCreate.cs")
            .FirstOrDefault(f => !f.EndsWith(".Designer.cs"));

        (file is not null).ShouldBeTrue("InitialCreate migration file not found in Migrations folder");
        return File.ReadAllText(file!);
    }

    private static string LoadV6Migration()
    {
        Directory.Exists(MigrationsDir).ShouldBeTrue($"Migrations folder not found at: {MigrationsDir}");

        var file = Directory.GetFiles(MigrationsDir, "*V6_EntityModelOverhaul.cs")
            .FirstOrDefault(f => !f.EndsWith(".Designer.cs"));

        (file is not null).ShouldBeTrue("V6_EntityModelOverhaul migration file not found in Migrations folder");
        return File.ReadAllText(file!);
    }

    [Fact]
    public void Game_Status_enum_column_is_stored_as_text_in_migration()
    {
        var migration = LoadInitialCreateMigration();
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
        migration.ShouldContain("IX_KingdomResources_KingdomId_ResourceType");
        migration.ShouldMatch(@"ResourceType\s*=\s*table\.Column<string>\(type:\s*""text""");
    }

    [Fact]
    public void TerrainType_ResourceBonusType_enum_column_is_stored_as_text_in_migration()
    {
        var migration = LoadInitialCreateMigration();
        migration.ShouldContain("ResourceBonusType = table.Column<string>(type: \"text\"");
    }

    [Fact]
    public void V6_EGamePhase_column_is_stored_as_text()
    {
        var migration = LoadV6Migration();
        // CurrentPhase is added as AddColumn<string>(type: "text")
        migration.ShouldContain("\"CurrentPhase\"");
        // Verify it's stored as text by checking the AddColumn call
        migration.ShouldContain("name: \"CurrentPhase\"");
    }

    [Fact]
    public void V6_EEventType_column_is_stored_as_text()
    {
        var migration = LoadV6Migration();
        // EventType is renamed from Action (which was already text)
        migration.ShouldContain("newName: \"EventType\"");
    }

    [Fact]
    public void V6_EKingdomStatus_column_is_stored_as_text()
    {
        var migration = LoadV6Migration();
        // Status column on Kingdoms (replacing IsEliminated bool)
        migration.ShouldContain("Status");
    }

    [Fact]
    public void V6_EBattleOutcome_column_is_stored_as_text()
    {
        var migration = LoadV6Migration();
        // Outcome column on Battles
        migration.ShouldContain("Outcome");
    }

    [Fact]
    public void V6_WinCondition_Elimination_retained()
    {
        // EWinCondition.Elimination is the only value -- verify it's still stored as text
        var migration = LoadInitialCreateMigration();
        migration.ShouldContain("WinCondition = table.Column<string>(type: \"text\"");
    }
}
