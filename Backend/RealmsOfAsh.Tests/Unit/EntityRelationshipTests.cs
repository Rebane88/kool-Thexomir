using Domain.Buildings;
using Domain.Map;
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
        var model = ctx.Model;
        var tileType = model.FindEntityType(typeof(Tile));
        tileType.ShouldNotBeNull();

        var gameFk = tileType!.GetForeignKeys()
            .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == "GameId"));

        gameFk.ShouldNotBeNull();
    }

    [Fact]
    public void Tile_has_TerrainTypeId_FK()
    {
        using var ctx = BuildContext();
        var model = ctx.Model;
        var tileType = model.FindEntityType(typeof(Tile));
        tileType.ShouldNotBeNull();

        var fk = tileType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "TerrainTypeId"));

        fk.ShouldNotBeNull();
    }

    [Fact]
    public void Kingdom_has_GameId_FK()
    {
        using var ctx = BuildContext();
        var model = ctx.Model;
        var kingdomType = model.FindEntityType(typeof(Domain.Game.Kingdom));
        kingdomType.ShouldNotBeNull();

        var fk = kingdomType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "GameId"));

        fk.ShouldNotBeNull();
    }

    [Fact]
    public void Building_has_TileId_FK()
    {
        using var ctx = BuildContext();
        var model = ctx.Model;
        var buildingType = model.FindEntityType(typeof(Building));
        buildingType.ShouldNotBeNull();

        var fk = buildingType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "TileId"));

        fk.ShouldNotBeNull();
    }

    [Fact]
    public void KingdomResource_has_KingdomId_FK()
    {
        using var ctx = BuildContext();
        var model = ctx.Model;
        var entityType = model.FindEntityType(typeof(KingdomResource));
        entityType.ShouldNotBeNull();

        var fk = entityType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "KingdomId"));

        fk.ShouldNotBeNull();
    }
}
