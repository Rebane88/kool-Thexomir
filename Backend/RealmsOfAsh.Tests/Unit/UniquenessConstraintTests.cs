using Domain.Factions;
using Domain.Game;
using Domain.Map;
using Domain.Military;
using Domain.Resources;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// INFRA-08: Uniqueness constraints are configured via HasIndex().IsUnique() in the EF model.
/// Verified via EF Core model metadata.
/// </summary>
[Collection("Database tests")]
public class UniquenessConstraintTests(DatabaseFixture fixture)
{
    private AppDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    private static bool HasUniqueIndex(AppDbContext ctx, Type entityClrType, params string[] propertyNames)
    {
        var entityType = ctx.Model.FindEntityType(entityClrType);
        if (entityType is null) return false;

        var nameSet = new HashSet<string>(propertyNames);

        return entityType.GetIndexes().Any(idx =>
            idx.IsUnique &&
            idx.Properties.Select(p => p.Name).ToHashSet().SetEquals(nameSet));
    }

    [Fact]
    public void UnitTypeMatchup_has_unique_index_on_AttackerTypeId_and_DefenderTypeId()
    {
        using var ctx = BuildContext();
        HasUniqueIndex(ctx, typeof(UnitTypeMatchup), "AttackerTypeId", "DefenderTypeId")
            .ShouldBeTrue("Expected unique index on UnitTypeMatchup(AttackerTypeId, DefenderTypeId)");
    }

    [Fact]
    public void FactionResourceBonus_has_unique_index_on_FactionTypeId_and_ResourceType()
    {
        using var ctx = BuildContext();
        HasUniqueIndex(ctx, typeof(FactionResourceBonus), "FactionTypeId", "ResourceType")
            .ShouldBeTrue("Expected unique index on FactionResourceBonus(FactionTypeId, ResourceType)");
    }

    [Fact]
    public void FactionUnitBonus_has_unique_index_on_FactionTypeId_and_UnitTypeId()
    {
        using var ctx = BuildContext();
        HasUniqueIndex(ctx, typeof(FactionUnitBonus), "FactionTypeId", "UnitTypeId")
            .ShouldBeTrue("Expected unique index on FactionUnitBonus(FactionTypeId, UnitTypeId)");
    }

    [Fact]
    public void KingdomResource_has_unique_index_on_KingdomId_and_ResourceType()
    {
        using var ctx = BuildContext();
        HasUniqueIndex(ctx, typeof(KingdomResource), "KingdomId", "ResourceType")
            .ShouldBeTrue("Expected unique index on KingdomResource(KingdomId, ResourceType)");
    }

    [Fact]
    public void Tile_has_unique_index_on_GameId_CoordQ_and_CoordR()
    {
        using var ctx = BuildContext();
        HasUniqueIndex(ctx, typeof(Tile), "GameId", "CoordQ", "CoordR")
            .ShouldBeTrue("Expected unique index on Tile(GameId, CoordQ, CoordR)");
    }

    [Fact]
    public void Game_has_unique_index_on_LobbyCode()
    {
        using var ctx = BuildContext();
        HasUniqueIndex(ctx, typeof(Game), "LobbyCode")
            .ShouldBeTrue("Expected unique index on Game(LobbyCode)");
    }
}
