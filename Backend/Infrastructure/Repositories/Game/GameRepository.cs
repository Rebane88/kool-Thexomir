using Domain.Game;

namespace Infrastructure.Repositories.Game;

public class GameRepository(AppDbContext context)
    : BaseRepository<Domain.Game.Game>(context), IGameRepository;
