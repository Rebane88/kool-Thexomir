using Domain.Military;

namespace Infrastructure.Repositories.Military;

public class UnitTypeMatchupRepository(AppDbContext context)
    : BaseRepository<UnitTypeMatchup>(context), IUnitTypeMatchupRepository;
