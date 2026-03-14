using Domain.Military;

namespace Infrastructure.Repositories.Military;

public class UnitTypeRepository(AppDbContext context)
    : BaseRepository<UnitType>(context), IUnitTypeRepository;
