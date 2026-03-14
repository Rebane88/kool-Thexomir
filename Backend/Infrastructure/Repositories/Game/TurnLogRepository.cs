using Domain.Game;

namespace Infrastructure.Repositories.Game;

public class TurnLogRepository(AppDbContext context)
    : BaseRepository<TurnLog>(context), ITurnLogRepository;
