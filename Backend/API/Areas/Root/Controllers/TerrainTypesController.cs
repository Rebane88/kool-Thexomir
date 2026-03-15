using Domain.Map;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Areas.Root.Controllers;

public class TerrainTypesController : ReferenceDataBaseController<TerrainType>
{
    public TerrainTypesController(AppDbContext context) : base(context) { }

    protected override DbSet<TerrainType> DbSet => _context.TerrainTypes;

    protected override string EntityDisplayName => "Terrain Type";

    protected override void PopulateEntity(TerrainType entity, IFormCollection form)
    {
        entity.DefenseBonus = decimal.TryParse(form["DefenseBonus"].ToString(), out var db) ? db : 0m;
        entity.MovementCost = int.TryParse(form["MovementCost"].ToString(), out var mc) ? mc : 1;
        entity.ResourceBonusType = Enum.TryParse<ETerrainResourceBonus>(
            form["ResourceBonusType"].ToString(), out var rbt) ? rbt : ETerrainResourceBonus.None;
    }

    protected override object ToViewModel(TerrainType entity) => new
    {
        entity.Id,
        NameEn = entity.Name.Translate("en") ?? string.Empty,
        entity.DefenseBonus,
        entity.MovementCost,
        entity.ResourceBonusType
    };
}
