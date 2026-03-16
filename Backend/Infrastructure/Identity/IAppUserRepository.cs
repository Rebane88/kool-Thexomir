using Base.Contracts;

namespace Infrastructure.Identity;

public interface IAppUserRepository : IBaseRepository<AppUser>
{
    Task<AppUser?> FindByEmailAsync(string email);
}
