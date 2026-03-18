using Domain.Military;

namespace Infrastructure.Repositories.Military;

public class BattleRoundRepository(AppDbContext context)
    : BaseRepository<BattleRound>(context), IBattleRoundRepository;
