using Domain.Military;

namespace Infrastructure.Repositories.Military;

public class ArmyRepository(AppDbContext context)
    : BaseRepository<Army>(context), IArmyRepository;
