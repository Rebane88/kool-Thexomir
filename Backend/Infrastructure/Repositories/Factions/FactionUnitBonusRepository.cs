using Domain.Factions;

namespace Infrastructure.Repositories.Factions;

public class FactionUnitBonusRepository(AppDbContext context)
    : BaseRepository<FactionUnitBonus>(context), IFactionUnitBonusRepository;
