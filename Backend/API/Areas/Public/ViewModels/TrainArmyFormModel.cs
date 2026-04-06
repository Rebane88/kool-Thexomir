using System.ComponentModel.DataAnnotations;

namespace API.Areas.Public.ViewModels;

public class TrainArmyFormModel
{
    [Required] public Guid BuildingId { get; set; }
    [Required] public Guid ArmyTypeId { get; set; }
}
