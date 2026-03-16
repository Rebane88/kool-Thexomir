using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Identity;

public class AppUserRepository(AppDbContext context)
    : BaseRepository<AppUser>(context), IAppUserRepository
{
    public async Task<AppUser?> FindByEmailAsync(string email)
    {
        return await Context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant());
    }
}
