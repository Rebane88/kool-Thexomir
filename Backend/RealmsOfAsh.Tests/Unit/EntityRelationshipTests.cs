using Domain.Buildings;
using Domain.Map;
using Domain.Military;
using Domain.Resources;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// INFRA-02: Key entities have proper FK properties and navigation properties.
/// Verified via EF Core model metadata (no DB rows required).
/// </summary>
[Collection("Database tests")]
public class EntityRelationshipTests(DatabaseFixture fixture)
{
    private AppDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void Tile_has_GameId_FK()
    {
        using var ctx = BuildContext();
        var tileType = ctx.Model.FindEntityType(typeof(Tile));
        tileType.ShouldNotBeNull();

        var gameFk = tileType!.GetForeignKeys()
            .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == "GameId"));
        gameFk.ShouldNotBeNull();
    }

    [Fact]
    public void Tile_has_TerrainTypeId_FK()
    {
        using var ctx = BuildContext();
        var tileType = ctx.Model.FindEntityType(typeof(Tile));
        tileType.ShouldNotBeNull();

        var fk = tileType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "TerrainTypeId"));
        fk.ShouldNotBeNull();
    }

    [Fact]
    public void Kingdom_has_GameId_FK()
    {
        using var ctx = BuildContext();
        var kingdomType = ctx.Model.FindEntityType(typeof(Domain.Game.Kingdom));
        kingdomType.ShouldNotBeNull();

        var fk = kingdomType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "GameId"));
        fk.ShouldNotBeNull();
    }

    [Fact]
    public void Building_has_TileId_FK()
    {
        using var ctx = BuildContext();
        var buildingType = ctx.Model.FindEntityType(typeof(Building));
        buildingType.ShouldNotBeNull();

        var fk = buildingType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "TileId"));
        fk.ShouldNotBeNull();
    }

    [Fact]
    public void KingdomResource_has_KingdomId_FK()
    {
        using var ctx = BuildContext();
        var entityType = ctx.Model.FindEntityType(typeof(KingdomResource));
        entityType.ShouldNotBeNull();

        var fk = entityType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "KingdomId"));
        fk.ShouldNotBeNull();
    }

    // --- New v6.0 entity relationship tests ---

    [Fact]
    public void Army_has_ArmyTypeId_FK()
    {
        using var ctx = BuildContext();
        var armyType = ctx.Model.FindEntityType(typeof(Army));
        armyType.ShouldNotBeNull();

        var fk = armyType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "ArmyTypeId"));
        fk.ShouldNotBeNull();
    }

    [Fact]
    public void Army_has_BuildingId_FK()
    {
        using var ctx = BuildContext();
        var armyType = ctx.Model.FindEntityType(typeof(Army));
        armyType.ShouldNotBeNull();

        var fk = armyType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "BuildingId"));
        fk.ShouldNotBeNull();
    }

    [Fact]
    public void BattleRound_has_BattleId_FK()
    {
        using var ctx = BuildContext();
        var brType = ctx.Model.FindEntityType(typeof(BattleRound));
        brType.ShouldNotBeNull();

        var fk = brType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "BattleId"));
        fk.ShouldNotBeNull();
    }

    [Fact]
    public void BattleRound_has_AttackerArmyId_FK()
    {
        using var ctx = BuildContext();
        var brType = ctx.Model.FindEntityType(typeof(BattleRound));
        brType.ShouldNotBeNull();

        var fk = brType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "AttackerArmyId"));
        fk.ShouldNotBeNull();
    }

    [Fact]
    public void BattleRound_has_DefenderArmyId_FK()
    {
        using var ctx = BuildContext();
        var brType = ctx.Model.FindEntityType(typeof(BattleRound));
        brType.ShouldNotBeNull();

        var fk = brType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "DefenderArmyId"));
        fk.ShouldNotBeNull();
    }

    [Fact]
    public void Battle_has_AttackerTileId_FK()
    {
        using var ctx = BuildContext();
        var battleType = ctx.Model.FindEntityType(typeof(Battle));
        battleType.ShouldNotBeNull();

        var fk = battleType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "AttackerTileId"));
        fk.ShouldNotBeNull();
    }

    [Fact]
    public void Battle_has_DefenderTileId_FK()
    {
        using var ctx = BuildContext();
        var battleType = ctx.Model.FindEntityType(typeof(Battle));
        battleType.ShouldNotBeNull();

        var fk = battleType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "DefenderTileId"));
        fk.ShouldNotBeNull();
    }
}
