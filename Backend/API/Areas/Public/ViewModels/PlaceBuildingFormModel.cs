using System.ComponentModel.DataAnnotations;

namespace API.Areas.Public.ViewModels;

public class PlaceBuildingFormModel
{
    [Required] public Guid TileId { get; set; }
    [Required] public Guid BuildingTypeId { get; set; }
}
