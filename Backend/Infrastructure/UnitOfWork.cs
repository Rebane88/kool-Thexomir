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

namespace Infrastructure;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
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
    private readonly Lazy<IUnitRepository> _units = new(() => new UnitRepository(context));
    private readonly Lazy<IUnitTypeRepository> _unitTypes = new(() => new UnitTypeRepository(context));
    private readonly Lazy<IUnitTypeMatchupRepository> _unitTypeMatchups = new(() => new UnitTypeMatchupRepository(context));
    private readonly Lazy<IBattleRepository> _battles = new(() => new BattleRepository(context));
    private readonly Lazy<IBuildingUnitTypeRepository> _buildingUnitTypes = new(() => new BuildingUnitTypeRepository(context));

    // Resources
    private readonly Lazy<IKingdomResourceRepository> _kingdomResources = new(() => new KingdomResourceRepository(context));

    // Factions
    private readonly Lazy<IFactionTypeRepository> _factionTypes = new(() => new FactionTypeRepository(context));
    private readonly Lazy<IFactionResourceBonusRepository> _factionResourceBonuses = new(() => new FactionResourceBonusRepository(context));
    private readonly Lazy<IFactionUnitBonusRepository> _factionUnitBonuses = new(() => new FactionUnitBonusRepository(context));

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
    public IUnitRepository Units => _units.Value;
    public IUnitTypeRepository UnitTypes => _unitTypes.Value;
    public IUnitTypeMatchupRepository UnitTypeMatchups => _unitTypeMatchups.Value;
    public IBattleRepository Battles => _battles.Value;
    public IBuildingUnitTypeRepository BuildingUnitTypes => _buildingUnitTypes.Value;
    public IKingdomResourceRepository KingdomResources => _kingdomResources.Value;
    public IFactionTypeRepository FactionTypes => _factionTypes.Value;
    public IFactionResourceBonusRepository FactionResourceBonuses => _factionResourceBonuses.Value;
    public IFactionUnitBonusRepository FactionUnitBonuses => _factionUnitBonuses.Value;

    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        try
        {
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
