using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.v1.Identity;

public class Login
{
    [MaxLength(256)]
    [EmailAddress]
    [Required]
    public string Email { get; set; } = default!;

    [MinLength(6)]
    [MaxLength(100)]
    [Required]
    public string Password { get; set; } = default!;
}
