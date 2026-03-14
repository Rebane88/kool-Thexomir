using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.v1.Identity;

public class LogoutInfo
{
    [MaxLength(128)]
    [Required]
    public string RefreshToken { get; set; } = default!;
}
