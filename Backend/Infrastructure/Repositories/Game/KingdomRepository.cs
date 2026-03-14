using Domain.Game;

namespace Infrastructure.Repositories.Game;

public class KingdomRepository(AppDbContext context)
    : BaseRepository<Kingdom>(context), IKingdomRepository;
