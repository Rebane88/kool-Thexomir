using Domain.Identity;

namespace Infrastructure.Repositories.Identity;

public class AppRefreshTokenRepository(AppDbContext context)
    : BaseRepository<AppRefreshToken>(context), IAppRefreshTokenRepository;
