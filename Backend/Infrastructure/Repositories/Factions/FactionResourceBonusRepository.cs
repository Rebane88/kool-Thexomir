using Domain.Factions;

namespace Infrastructure.Repositories.Factions;

public class FactionResourceBonusRepository(AppDbContext context)
    : BaseRepository<FactionResourceBonus>(context), IFactionResourceBonusRepository;
