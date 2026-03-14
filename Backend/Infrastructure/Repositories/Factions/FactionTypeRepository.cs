using Domain.Factions;

namespace Infrastructure.Repositories.Factions;

public class FactionTypeRepository(AppDbContext context)
    : BaseRepository<FactionType>(context), IFactionTypeRepository;
