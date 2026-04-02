using Application.Contracts;
using Base.Contracts;
using Domain.Buildings;
using Domain.Factions;
using Domain.Game;
using Domain.Map;
using Domain.Military;
using Domain.Resources;
using Infrastructure.Repositories.Buildings;
using Infrastructure.Repositories.Factions;
using Infrastructure.Repositories.Game;
using Infrastructure.Repositories.Map;
using Infrastructure.Repositories.Military;
using Infrastructure.Repositories.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

public class UnitOfWork(AppDbContext context, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    // Game
    private readonly Lazy<IGameRepository> _games = new(() => new GameRepository(context));
    private readonly Lazy<IKingdomRepository> _kingdoms = new(() => new KingdomRepository(context));
    private readonly Lazy<ITurnLogRepository> _turnLogs = new(() => new TurnLogRepository(context));
    private readonly Lazy<IGameEventRepository> _gameEvents = new(() => new GameEventRepository(context));

    // Map
    private readonly Lazy<ITileRepository> _tiles = new(() => new TileRepository(context));
    private readonly Lazy<ITerrainTypeRepository> _terrainTypes = new(() => new TerrainTypeRepository(context));

    // Buildings
    private readonly Lazy<IBuildingRepository> _buildings = new(() => new BuildingRepository(context));
    private readonly Lazy<IBuildingTypeRepository> _buildingTypes = new(() => new BuildingTypeRepository(context));

    // Military
    private readonly Lazy<IArmyRepository> _armies = new(() => new ArmyRepository(context));
    private readonly Lazy<IArmyTypeRepository> _armyTypes = new(() => new ArmyTypeRepository(context));
    private readonly Lazy<IBattleRepository> _battles = new(() => new BattleRepository(context));
    private readonly Lazy<IBattleRoundRepository> _battleRounds = new(() => new BattleRoundRepository(context));
    private readonly Lazy<IDeclaredAttackRepository> _declaredAttacks = new(() => new DeclaredAttackRepository(context));

    // Resources
    private readonly Lazy<IKingdomResourceRepository> _kingdomResources = new(() => new KingdomResourceRepository(context));

    // Factions
    private readonly Lazy<IFactionTypeRepository> _factionTypes = new(() => new FactionTypeRepository(context));

    // Properties
    public IGameRepository Games => _games.Value;
    public IKingdomRepository Kingdoms => _kingdoms.Value;
    public ITurnLogRepository TurnLogs => _turnLogs.Value;
    public IGameEventRepository GameEvents => _gameEvents.Value;
    public ITileRepository Tiles => _tiles.Value;
    public ITerrainTypeRepository TerrainTypes => _terrainTypes.Value;
    public IBuildingRepository Buildings => _buildings.Value;
    public IBuildingTypeRepository BuildingTypes => _buildingTypes.Value;
    public IArmyRepository Armies => _armies.Value;
    public IArmyTypeRepository ArmyTypes => _armyTypes.Value;
    public IBattleRepository Battles => _battles.Value;
    public IBattleRoundRepository BattleRounds => _battleRounds.Value;
    public IDeclaredAttackRepository DeclaredAttacks => _declaredAttacks.Value;
    public IKingdomResourceRepository KingdomResources => _kingdomResources.Value;
    public IFactionTypeRepository FactionTypes => _factionTypes.Value;

    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        try
        {
            // Log Kingdom changes before saving
            foreach (var entry in context.ChangeTracker.Entries<Kingdom>())
            {
                if (entry.State == EntityState.Modified)
                {
                    var orig = entry.OriginalValues["ConsecutiveMissedTurns"];
                    var curr = entry.CurrentValues["ConsecutiveMissedTurns"];
                    logger.LogWarning("[UoW] Kingdom {Id} state=Modified: ConsecutiveMissedTurns {Orig} -> {Curr}",
                        entry.Entity.Id, orig, curr);
                }
                else
                {
                    logger.LogInformation("[UoW] Kingdom {Id} state={State} ConsecutiveMissedTurns={Val}",
                        entry.Entity.Id, entry.State, entry.Entity.ConsecutiveMissedTurns);
                }
            }
            return await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException(ex.Message);
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            entry.State = EntityState.Detached;
        }
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        context.Dispose();
    }
}
