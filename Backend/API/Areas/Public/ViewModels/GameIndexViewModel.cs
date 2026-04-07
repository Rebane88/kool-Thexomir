using Application.Services.Army.DTOs;
using Application.Services.Building.DTOs;
using Application.Services.Combat.DTOs;
using Application.Services.GameInitialization.DTOs;
using Application.Services.WinCondition.DTOs;

namespace API.Areas.Public.ViewModels;

/// <summary>
/// Composite ViewModel consumed by GameController.Index and every Game/*.cshtml view.
/// Each later plan in Phase 37 fills in the child VMs and binds data to this skeleton.
/// </summary>
public class GameIndexViewModel
{
    public Guid GameId { get; set; }
    public Guid MyUserId { get; set; }
    public Guid? SelectedTileId { get; set; }

    public GameStateDto State { get; set; } = null!;
    public HexLayoutViewModel Layout { get; set; } = null!;
    public List<BuildingCatalogEntryViewModel> Catalog { get; set; } = new();
    public List<ArmyTypeDto> ArmyTypes { get; set; } = new();

    public BattleResultDto? LastBattleResult { get; set; }
    public GameOverDto? GameOverData { get; set; }
    public string? AttackError { get; set; }

    public KingdomDto? MyKingdom =>
        State.Kingdoms.FirstOrDefault(k => k.UserId == MyUserId);

    public bool IsMyTurn =>
        MyKingdom is not null && State.CurrentTurnKingdomId == MyKingdom.Id;

    /// <summary>
    /// Returns the catalog entries visible for the given tile, applying the tier-aware
    /// filter mirroring BuildingPanel.tsx:
    ///   - Empty tile → show only Tier 1 entries
    ///   - Tile with a building → show only entries in the same chain with a higher tier
    /// </summary>
    public IEnumerable<BuildingCatalogEntryViewModel> IsBuildingAvailable(TileDto tile)
    {
        if (tile.Buildings.Count == 0)
            return Catalog.Where(c => c.BuildingType.Tier == 1);
        var existingInstance = tile.Buildings[0];
        var existingType = Catalog.FirstOrDefault(c => c.BuildingType.Id == existingInstance.BuildingTypeId)?.BuildingType;
        if (existingType is null) return Enumerable.Empty<BuildingCatalogEntryViewModel>();
        return Catalog.Where(c => c.BuildingType.Chain == existingType.Chain && c.BuildingType.Tier > existingType.Tier);
    }

    /// <summary>
    /// Finds the tile that contains a building whose id matches army.BuildingId.
    /// ArmyDto has no TileId; garrison location is inferred by scanning Tile.Buildings.
    /// </summary>
    public TileDto? FindTileForArmy(ArmyDto army)
    {
        foreach (var tile in State.Tiles)
            if (tile.Buildings.Any(b => b.Id == army.BuildingId))
                return tile;
        return null;
    }

    /// <summary>
    /// Determines the current battle wizard step for the active player based on
    /// DeclaredAttacks flags in GameStateDto. Returns None for non-battle phases or spectators.
    /// </summary>
    public BattleWizardStep GetBattleStep()
    {
        if (GameOverData is not null) return BattleWizardStep.None;
        if (LastBattleResult is not null) return BattleWizardStep.PostBattle;
        if (State.CurrentPhase != "Battle") return BattleWizardStep.None;
        if (MyKingdom is null) return BattleWizardStep.None;

        var myAttack = State.DeclaredAttacks.FirstOrDefault(
            da => da.AttackerKingdomId == MyKingdom.Id || da.DefenderKingdomId == MyKingdom.Id);
        if (myAttack is null) return BattleWizardStep.None;

        bool amAttacker = myAttack.AttackerKingdomId == MyKingdom.Id;
        bool iSelected = amAttacker ? myAttack.AttackerArmiesSelected : myAttack.DefenderArmiesSelected;
        bool opponentSelected = amAttacker ? myAttack.DefenderArmiesSelected : myAttack.AttackerArmiesSelected;
        bool iConfirmed = amAttacker ? myAttack.AttackerLineupConfirmed : myAttack.DefenderLineupConfirmed;
        bool opponentConfirmed = amAttacker ? myAttack.DefenderLineupConfirmed : myAttack.AttackerLineupConfirmed;

        if (!iSelected) return BattleWizardStep.SelectArmies;
        if (!opponentSelected) return BattleWizardStep.WaitForOpponentSelect;
        if (!iConfirmed) return BattleWizardStep.SetLineup;
        if (!opponentConfirmed) return BattleWizardStep.WaitForOpponentLineup;
        return BattleWizardStep.WaitingForResolution;
    }
}

/// <summary>
/// Stub — Plan 02 ports HexLayout.cs and fills CentersByTileId / CornersByTileId.
/// The nested Point record is a local placeholder; Plan 02 will replace it with
/// the real HexLayout.Point from the Helpers namespace.
/// </summary>
public class HexLayoutViewModel
{
    public record Point(double X, double Y);

    public double ViewBoxWidth { get; set; }
    public double ViewBoxHeight { get; set; }
    public Dictionary<Guid, Point> CentersByTileId { get; set; } = new();
    public Dictionary<Guid, List<Point>> CornersByTileId { get; set; } = new();
}

public enum BattleWizardStep
{
    None,
    DeclareAttack,
    SelectArmies,
    WaitForOpponentSelect,
    SetLineup,
    WaitForOpponentLineup,
    WaitingForResolution,
    PostBattle
}

/// <summary>
/// Stub — Plan 03 fills CanAfford / PrereqMet affordability logic.
/// </summary>
public class BuildingCatalogEntryViewModel
{
    public BuildingTypeDto BuildingType { get; set; } = null!;
    public bool CanAfford { get; set; }
    public bool PrereqMet { get; set; }
}
