using Domain.Buildings;

namespace Infrastructure.Repositories.Buildings;

public class BuildingRepository(AppDbContext context)
    : BaseRepository<Building>(context), IBuildingRepository;
