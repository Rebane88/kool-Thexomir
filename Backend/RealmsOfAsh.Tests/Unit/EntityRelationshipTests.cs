using Domain.Buildings;
using Domain.Factions;
using Domain.Map;
using Domain.Military;
using Domain.Resources;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// INFRA-02: Key entities have proper FK properties and navigation properties.
/// Verified via EF Core InMemory model metadata (no DB required).
/// </summary>
public class EntityRelationshipTests
{
    private static AppDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void Tile_has_GameId_FK()
    {
        using var ctx = BuildContext();
        var model = ctx.Model;
        var tileType = model.FindEntityType(typeof(Tile));
        Assert.NotNull(tileType);

        var gameFk = tileType!.GetForeignKeys()
            .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == "GameId"));

        Assert.NotNull(gameFk);
    }

    [Fact]
    public void Tile_has_TerrainTypeId_FK()
    {
        using var ctx = BuildContext();
        var model = ctx.Model;
        var tileType = model.FindEntityType(typeof(Tile));
        Assert.NotNull(tileType);

        var fk = tileType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "TerrainTypeId"));

        Assert.NotNull(fk);
    }

    [Fact]
    public void Kingdom_has_GameId_FK()
    {
        using var ctx = BuildContext();
        var model = ctx.Model;
        var kingdomType = model.FindEntityType(typeof(Domain.Game.Kingdom));
        Assert.NotNull(kingdomType);

        var fk = kingdomType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "GameId"));

        Assert.NotNull(fk);
    }

    [Fact]
    public void Building_has_TileId_FK()
    {
        using var ctx = BuildContext();
        var model = ctx.Model;
        var buildingType = model.FindEntityType(typeof(Building));
        Assert.NotNull(buildingType);

        var fk = buildingType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "TileId"));

        Assert.NotNull(fk);
    }

    [Fact]
    public void UnitTypeMatchup_has_AttackerTypeId_and_DefenderTypeId_FKs()
    {
        using var ctx = BuildContext();
        var model = ctx.Model;
        var matchupType = model.FindEntityType(typeof(UnitTypeMatchup));
        Assert.NotNull(matchupType);

        var allFkProps = matchupType!.GetForeignKeys()
            .SelectMany(fk => fk.Properties)
            .Select(p => p.Name)
            .ToHashSet();

        Assert.Contains("AttackerTypeId", allFkProps);
        Assert.Contains("DefenderTypeId", allFkProps);
    }

    [Fact]
    public void FactionResourceBonus_has_FactionTypeId_FK()
    {
        using var ctx = BuildContext();
        var model = ctx.Model;
        var entityType = model.FindEntityType(typeof(FactionResourceBonus));
        Assert.NotNull(entityType);

        var fk = entityType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "FactionTypeId"));

        Assert.NotNull(fk);
    }

    [Fact]
    public void KingdomResource_has_KingdomId_FK()
    {
        using var ctx = BuildContext();
        var model = ctx.Model;
        var entityType = model.FindEntityType(typeof(KingdomResource));
        Assert.NotNull(entityType);

        var fk = entityType!.GetForeignKeys()
            .FirstOrDefault(f => f.Properties.Any(p => p.Name == "KingdomId"));

        Assert.NotNull(fk);
    }
}
