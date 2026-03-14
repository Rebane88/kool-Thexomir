using Domain.Buildings;

namespace Infrastructure.Repositories.Buildings;

public class BuildingTypeRepository(AppDbContext context)
    : BaseRepository<BuildingType>(context), IBuildingTypeRepository;
