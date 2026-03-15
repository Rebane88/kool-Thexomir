using Domain.Buildings;
using Domain.Factions;
using Domain.Game;
using Domain.Identity;
using Domain.Map;
using Domain.Military;
using Domain.Resources;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using MilitaryUnit = Domain.Military.Unit;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// INFRA-03: AppDbContext has DbSet properties registered for all 18 game entities.
/// Verified by checking the EF Core model entity type registry.
/// </summary>
public class AppDbContextTests
{
    private static AppDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static void AssertDbSetRegistered<TEntity>(AppDbContext ctx) where TEntity : class
    {
        var entityType = ctx.Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entityType);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_Game()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<Game>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_Kingdom()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<Kingdom>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_Tile()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<Tile>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_TerrainType()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<TerrainType>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_Building()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<Building>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_BuildingType()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<BuildingType>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_Army()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<Army>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_Unit()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<MilitaryUnit>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_UnitType()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<UnitType>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_UnitTypeMatchup()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<UnitTypeMatchup>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_Battle()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<Battle>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_KingdomResource()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<KingdomResource>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_TurnLog()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<TurnLog>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_GameEvent()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<GameEvent>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_FactionType()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<FactionType>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_FactionResourceBonus()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<FactionResourceBonus>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_FactionUnitBonus()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<FactionUnitBonus>(ctx);
    }

    [Fact]
    public void AppDbContext_has_DbSet_for_AppRefreshToken_Identity()
    {
        using var ctx = BuildContext();
        AssertDbSetRegistered<AppRefreshToken>(ctx);
    }
}
