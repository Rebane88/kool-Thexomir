namespace Application.Services.Military.DTOs;

public class ArmyMovedDto
{
    public Guid ArmyId { get; set; }
    public Guid FromTileId { get; set; }
    public Guid ToTileId { get; set; }
    public Guid KingdomId { get; set; }
    public bool TileClaimed { get; set; }
    public bool ArmyMerged { get; set; }
    public Guid? MergedIntoArmyId { get; set; }
}
