using System.ComponentModel.DataAnnotations;

namespace API.Areas.Public.ViewModels;

public class SelectFactionViewModel
{
    [Required]
    public Guid LobbyId { get; set; }

    [Required]
    public Guid FactionTypeId { get; set; }
}
