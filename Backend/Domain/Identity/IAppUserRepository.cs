using Base.Contracts;

namespace Domain.Identity;

public interface IAppUserRepository : IBaseRepository<AppUser>
{
    // Domain-specific queries added in feature phases
    Task<AppUser?> FindByEmailAsync(string email);
}
