using Base.Contracts;
using Domain.Buildings;
using Domain.Factions;
using Domain.Game;
using Domain.Identity;
using Domain.Map;
using Domain.Military;
using Domain.Resources;

namespace Application.Contracts;

public interface IUnitOfWork : IDisposable
{
    // Identity
    IAppUserRepository Users { get; }
    IAppRefreshTokenRepository RefreshTokens { get; }

    // Game
    IGameRepository Games { get; }
    IKingdomRepository Kingdoms { get; }
    ITurnLogRepository TurnLogs { get; }
    IGameEventRepository GameEvents { get; }

    // Map
    ITileRepository Tiles { get; }
    ITerrainTypeRepository TerrainTypes { get; }

    // Buildings
    IBuildingRepository Buildings { get; }
    IBuildingTypeRepository BuildingTypes { get; }

    // Military
    IArmyRepository Armies { get; }
    IUnitRepository Units { get; }
    IUnitTypeRepository UnitTypes { get; }
    IUnitTypeMatchupRepository UnitTypeMatchups { get; }
    IBattleRepository Battles { get; }

    // Resources
    IKingdomResourceRepository KingdomResources { get; }

    // Factions
    IFactionTypeRepository FactionTypes { get; }
    IFactionResourceBonusRepository FactionResourceBonuses { get; }
    IFactionUnitBonusRepository FactionUnitBonuses { get; }

    Task<int> CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
