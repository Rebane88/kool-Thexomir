using Base;
using Domain.Buildings;
using Domain.Factions;
using Domain.Game;
using Infrastructure.Identity;
using Domain.Map;
using Domain.Military;
using Domain.Resources;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser, AppRole, Guid>(options), IDataProtectionKeyContext
{
    // Identity
    public DbSet<AppRefreshToken> RefreshTokens { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    // Game
    public DbSet<Game> Games { get; set; }
    public DbSet<Kingdom> Kingdoms { get; set; }
    public DbSet<TurnLog> TurnLogs { get; set; }
    public DbSet<GameEvent> GameEvents { get; set; }

    // Map
    public DbSet<Tile> Tiles { get; set; }
    public DbSet<TerrainType> TerrainTypes { get; set; }

    // Buildings
    public DbSet<Building> Buildings { get; set; }
    public DbSet<BuildingType> BuildingTypes { get; set; }

    // Military
    public DbSet<Army> Armies { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<UnitType> UnitTypes { get; set; }
    public DbSet<UnitTypeMatchup> UnitTypeMatchups { get; set; }
    public DbSet<Battle> Battles { get; set; }
    public DbSet<BuildingUnitType> BuildingUnitTypes { get; set; }

    // Resources
    public DbSet<KingdomResource> KingdomResources { get; set; }

    // Factions
    public DbSet<FactionType> FactionTypes { get; set; }
    public DbSet<FactionResourceBonus> FactionResourceBonuses { get; set; }
    public DbSet<FactionUnitBonus> FactionUnitBonuses { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure all DateTime properties to use UTC
        ConfigureDateTimeAsUtc(builder);

        // Enum string conversions (INFRA-07)
        builder.Entity<Game>()
            .Property(g => g.Status)
            .HasConversion<string>();

        builder.Entity<Game>()
            .Property(g => g.WinCondition)
            .HasConversion<string>();

        builder.Entity<KingdomResource>()
            .Property(kr => kr.ResourceType)
            .HasConversion<string>();

        builder.Entity<TerrainType>()
            .Property(t => t.ResourceBonusType)
            .HasConversion<string>();

        builder.Entity<FactionResourceBonus>()
            .Property(b => b.ResourceType)
            .HasConversion<string>();

        // Uniqueness constraints (INFRA-08)
        builder.Entity<UnitTypeMatchup>()
            .HasIndex(m => new { m.AttackerTypeId, m.DefenderTypeId })
            .IsUnique();

        builder.Entity<FactionResourceBonus>()
            .HasIndex(b => new { b.FactionTypeId, b.ResourceType })
            .IsUnique();

        builder.Entity<FactionUnitBonus>()
            .HasIndex(b => new { b.FactionTypeId, b.UnitTypeId })
            .IsUnique();

        builder.Entity<KingdomResource>()
            .HasIndex(kr => new { kr.KingdomId, kr.ResourceType })
            .IsUnique();

        builder.Entity<Tile>()
            .HasIndex(t => new { t.GameId, t.CoordQ, t.CoordR })
            .IsUnique();

        builder.Entity<Tile>()
            .Property(t => t.IsCapital)
            .HasDefaultValue(false);

        builder.Entity<Game>()
            .HasIndex(g => g.LobbyCode)
            .IsUnique();

        // xmin concurrency token for lobby join race condition protection (Npgsql-native)
        // uint property named xmin maps to PostgreSQL's system column; IsRowVersion() marks it as concurrency token
        builder.Entity<Game>()
            .Property(g => g.xmin)
            .HasColumnName("xmin")
            .IsRowVersion();

        // HostUserId FK to AspNetUsers (no navigation property — Domain has no reference to Identity)
        builder.Entity<Game>()
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(g => g.HostUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // CurrentTurnKingdomId FK to Kingdom
        builder.Entity<Game>()
            .HasOne(g => g.CurrentTurnKingdom)
            .WithMany()
            .HasForeignKey(g => g.CurrentTurnKingdomId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // WinnerKingdomId FK to Kingdom (optional)
        builder.Entity<Game>()
            .HasOne(g => g.WinnerKingdom)
            .WithMany()
            .HasForeignKey(g => g.WinnerKingdomId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // One building per tile
        builder.Entity<Building>()
            .HasIndex(b => b.TileId)
            .IsUnique();

        // FK configuration for tricky relationships

        // UnitTypeMatchup: two FKs to UnitType — EF Core can't auto-resolve
        builder.Entity<UnitTypeMatchup>()
            .HasOne(m => m.AttackerType)
            .WithMany(u => u.AttackerMatchups)
            .HasForeignKey(m => m.AttackerTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UnitTypeMatchup>()
            .HasOne(m => m.DefenderType)
            .WithMany(u => u.DefenderMatchups)
            .HasForeignKey(m => m.DefenderTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // FactionUnitBonus: nullable FK to UnitType
        builder.Entity<FactionUnitBonus>()
            .HasOne(b => b.UnitType)
            .WithMany()
            .HasForeignKey(b => b.UnitTypeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Battle: two FKs to Army
        builder.Entity<Battle>()
            .HasOne(b => b.AttackerArmy)
            .WithMany()
            .HasForeignKey(b => b.AttackerArmyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Battle>()
            .HasOne(b => b.DefenderArmy)
            .WithMany()
            .HasForeignKey(b => b.DefenderArmyId)
            .OnDelete(DeleteBehavior.Restrict);

        // BuildingUnitType: junction table for building->unit type mapping
        builder.Entity<BuildingUnitType>(entity =>
        {
            entity.HasIndex(e => new { e.BuildingTypeId, e.UnitTypeId }).IsUnique();
            entity.HasOne(e => e.BuildingType).WithMany().HasForeignKey(e => e.BuildingTypeId);
            entity.HasOne(e => e.UnitType).WithMany().HasForeignKey(e => e.UnitTypeId);
        });

        // BuildingType: self-reference (upgrade chain)
        builder.Entity<BuildingType>()
            .HasOne(bt => bt.PrerequisiteBuildingType)
            .WithMany()
            .HasForeignKey(bt => bt.PrerequisiteBuildingTypeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // KingdomResource: Amount precision
        builder.Entity<KingdomResource>()
            .Property(kr => kr.Amount)
            .HasPrecision(18, 2);

        // AppUserId FK on Kingdom to AspNetUsers (no navigation property — Domain has no reference to Identity)
        builder.Entity<Kingdom>()
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(k => k.AppUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Disable cascade delete globally
        foreach (var relationship in builder.Model
                     .GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<IBaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Configures all DateTime and DateTime? properties to convert to UTC when saving to PostgreSQL.
    /// PostgreSQL's 'timestamp with time zone' type requires UTC values.
    /// </summary>
    private static void ConfigureDateTimeAsUtc(ModelBuilder builder)
    {
        // Value converter for DateTime
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(v, DateTimeKind.Utc)
                : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // Value converter for DateTime?
        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                    : v.Value.ToUniversalTime())
                : v,
            v => v.HasValue
                ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                : v);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }
    }
}
