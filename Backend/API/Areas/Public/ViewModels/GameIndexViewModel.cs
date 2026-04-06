using Application.Services.Army.DTOs;
using Application.Services.Building.DTOs;
using Application.Services.GameInitialization.DTOs;

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

    public KingdomDto? MyKingdom =>
        State.Kingdoms.FirstOrDefault(k => k.UserId == MyUserId);

    public bool IsMyTurn =>
        MyKingdom is not null && State.CurrentTurnKingdomId == MyKingdom.Id;

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

/// <summary>
/// Stub — Plan 03 fills CanAfford / PrereqMet affordability logic.
/// </summary>
public class BuildingCatalogEntryViewModel
{
    public BuildingTypeDto BuildingType { get; set; } = null!;
    public bool CanAfford { get; set; }
    public bool PrereqMet { get; set; }
}
