using Base;
using Microsoft.AspNetCore.Identity;

namespace Domain.Identity;

public class AppRole : IdentityRole<Guid>, IBaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
