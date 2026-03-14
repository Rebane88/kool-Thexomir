using Domain.Resources;

namespace Infrastructure.Repositories.Resources;

public class KingdomResourceRepository(AppDbContext context)
    : BaseRepository<KingdomResource>(context), IKingdomResourceRepository;
