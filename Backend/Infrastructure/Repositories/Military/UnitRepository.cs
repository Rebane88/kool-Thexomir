using Domain.Military;

namespace Infrastructure.Repositories.Military;

public class UnitRepository(AppDbContext context)
    : BaseRepository<Unit>(context), IUnitRepository;
