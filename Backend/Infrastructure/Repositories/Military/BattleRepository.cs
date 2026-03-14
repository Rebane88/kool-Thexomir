using Domain.Military;

namespace Infrastructure.Repositories.Military;

public class BattleRepository(AppDbContext context)
    : BaseRepository<Battle>(context), IBattleRepository;
