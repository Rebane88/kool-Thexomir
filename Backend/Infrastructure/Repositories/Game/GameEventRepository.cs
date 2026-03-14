using Domain.Game;

namespace Infrastructure.Repositories.Game;

public class GameEventRepository(AppDbContext context)
    : BaseRepository<GameEvent>(context), IGameEventRepository;
