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
    public DbSet<ArmyType> ArmyTypes { get; set; }
    public DbSet<Battle> Battles { get; set; }
    public DbSet<BattleRound> BattleRounds { get; set; }
    public DbSet<DeclaredAttack> DeclaredAttacks { get; set; }

    // Resources
    public DbSet<KingdomResource> KingdomResources { get; set; }

    // Factions
    public DbSet<FactionType> FactionTypes { get; set; }

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

        builder.Entity<Game>()
            .Property(g => g.CurrentPhase)
            .HasConversion<string>();

        builder.Entity<Kingdom>()
            .Property(k => k.Status)
            .HasConversion<string>();

        builder.Entity<TurnLog>()
            .Property(tl => tl.EventType)
            .HasConversion<string>();

        builder.Entity<Battle>()
            .Property(b => b.Outcome)
            .HasConversion<string>();

        builder.Entity<ArmyType>()
            .Property(at => at.SituationalBonusCondition)
            .HasConversion<string>();

        builder.Entity<FactionType>()
            .Property(ft => ft.StartingBonusResource)
            .HasConversion<string>();

        builder.Entity<KingdomResource>()
            .Property(kr => kr.ResourceType)
            .HasConversion<string>();

        builder.Entity<TerrainType>()
            .Property(t => t.ResourceBonusType)
            .HasConversion<string>();

        // Stable slug identity for asset lookups (see Domain/Map/TerrainType.cs)
        builder.Entity<TerrainType>()
            .Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(64);
        builder.Entity<TerrainType>()
            .HasIndex(t => t.Code)
            .IsUnique();

        // Stable slug identity for asset lookups (see Domain/Buildings/BuildingType.cs)
        builder.Entity<BuildingType>()
            .Property(bt => bt.Code)
            .IsRequired()
            .HasMaxLength(64);
        builder.Entity<BuildingType>()
            .HasIndex(bt => bt.Code)
            .IsUnique();

        // Uniqueness constraints (INFRA-08)
        builder.Entity<KingdomResource>()
            .HasIndex(kr => new { kr.KingdomId, kr.ResourceType })
            .IsUnique();

        builder.Entity<Tile>()
            .HasIndex(t => new { t.GameId, t.CoordQ, t.CoordR })
            .IsUnique();

        builder.Entity<Tile>()
            .Property(t => t.IsCastle)
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

        // Battle: multiple FKs to Kingdom
        builder.Entity<Battle>()
            .HasOne(b => b.AttackerKingdom)
            .WithMany()
            .HasForeignKey(b => b.AttackerKingdomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Battle>()
            .HasOne(b => b.DefenderKingdom)
            .WithMany()
            .HasForeignKey(b => b.DefenderKingdomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Battle: multiple FKs to Tile
        builder.Entity<Battle>()
            .HasOne(b => b.AttackerTile)
            .WithMany()
            .HasForeignKey(b => b.AttackerTileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Battle>()
            .HasOne(b => b.DefenderTile)
            .WithMany()
            .HasForeignKey(b => b.DefenderTileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Battle>()
            .HasOne(b => b.TileCaptured)
            .WithMany()
            .HasForeignKey(b => b.TileCapturedId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Battle>()
            .HasOne(b => b.TileCapturedFromKingdom)
            .WithMany()
            .HasForeignKey(b => b.TileCapturedFromKingdomId)
            .OnDelete(DeleteBehavior.Restrict);

        // BattleRound: army columns are historical references (no FK constraints)
        // Armies may be destroyed during combat, so these are plain Guid columns
        builder.Entity<BattleRound>()
            .Property(br => br.AttackerArmyId);
        builder.Entity<BattleRound>()
            .Property(br => br.DefenderArmyId);
        builder.Entity<BattleRound>()
            .Property(br => br.ArmyDestroyedId);

        builder.Entity<BattleRound>()
            .Property(br => br.InitiativeWinner)
            .HasConversion<string>();

        // BattleRound: unique compound index
        builder.Entity<BattleRound>()
            .HasIndex(br => new { br.BattleId, br.RoundNumber })
            .IsUnique();

        // BuildingType: self-reference (upgrade chain)
        builder.Entity<BuildingType>()
            .HasOne(bt => bt.UnlockedByBuildingType)
            .WithMany()
            .HasForeignKey(bt => bt.UnlockedByBuildingTypeId)
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

        // TurnLog: query by game + round
        builder.Entity<TurnLog>()
            .HasIndex(tl => new { tl.GameId, tl.RoundNumber });

        // TurnLog: query by game + kingdom
        builder.Entity<TurnLog>()
            .HasIndex(tl => new { tl.GameId, tl.KingdomId });

        // Army: query by building (capacity check)
        builder.Entity<Army>()
            .HasIndex(a => a.BuildingId);

        // Army: query by kingdom
        builder.Entity<Army>()
            .HasIndex(a => a.KingdomId);

        // DeclaredAttack: multiple FKs to Kingdom and Tile
        builder.Entity<DeclaredAttack>(entity =>
        {
            entity.HasOne(d => d.Game).WithMany().HasForeignKey(d => d.GameId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.AttackerKingdom).WithMany().HasForeignKey(d => d.AttackerKingdomId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.DefenderKingdom).WithMany().HasForeignKey(d => d.DefenderKingdomId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.TargetTile).WithMany().HasForeignKey(d => d.TargetTileId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.RiskedTile).WithMany().HasForeignKey(d => d.RiskedTileId).OnDelete(DeleteBehavior.Restrict);
        });

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
