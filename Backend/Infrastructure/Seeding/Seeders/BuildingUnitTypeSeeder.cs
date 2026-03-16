using Base.Contracts;
using Domain.Military;

namespace Infrastructure.Seeding.Seeders;

public class BuildingUnitTypeSeeder : ISeeder
{
    public int Order => 4;

    public void Seed(object context)
    {
        var db = (AppDbContext)context;
        if (db.BuildingUnitTypes.Any()) return;

        var now = DateTime.UtcNow;

        db.BuildingUnitTypes.AddRange(
            // Barracks -> Swordsman, Archer
            new BuildingUnitType { Id = Guid.NewGuid(), BuildingTypeId = BuildingTypeSeeder.BarracksId, UnitTypeId = UnitTypeSeeder.SwordsmanId, CreatedAt = now, UpdatedAt = now },
            new BuildingUnitType { Id = Guid.NewGuid(), BuildingTypeId = BuildingTypeSeeder.BarracksId, UnitTypeId = UnitTypeSeeder.ArcherId, CreatedAt = now, UpdatedAt = now },
            // Stables -> Knight
            new BuildingUnitType { Id = Guid.NewGuid(), BuildingTypeId = BuildingTypeSeeder.StablesId, UnitTypeId = UnitTypeSeeder.KnightId, CreatedAt = now, UpdatedAt = now },
            // War Academy -> Mage, Catapult
            new BuildingUnitType { Id = Guid.NewGuid(), BuildingTypeId = BuildingTypeSeeder.WarAcademyId, UnitTypeId = UnitTypeSeeder.MageId, CreatedAt = now, UpdatedAt = now },
            new BuildingUnitType { Id = Guid.NewGuid(), BuildingTypeId = BuildingTypeSeeder.WarAcademyId, UnitTypeId = UnitTypeSeeder.CatapultId, CreatedAt = now, UpdatedAt = now }
        );

        db.SaveChanges();
    }
}
