using Domain.Military;

namespace Infrastructure.Repositories.Military;

public class ArmyTypeRepository(AppDbContext context)
    : BaseRepository<ArmyType>(context), IArmyTypeRepository;
