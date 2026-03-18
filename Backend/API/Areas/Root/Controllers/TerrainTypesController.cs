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
        entity.ResourceMultiplier = decimal.TryParse(form["ResourceMultiplier"].ToString(), out var rm) ? rm : 1.10m;
        entity.ResourceBonusType = Enum.TryParse<ETerrainResourceBonus>(
            form["ResourceBonusType"].ToString(), out var rbt) ? rbt : ETerrainResourceBonus.None;
        entity.MapColor = form["MapColor"].ToString() is { Length: > 0 } mc ? mc : string.Empty;
    }

    protected override object ToViewModel(TerrainType entity) => new
    {
        entity.Id,
        NameEn = entity.Name.Translate("en") ?? string.Empty,
        entity.ResourceMultiplier,
        entity.ResourceBonusType,
        entity.MapColor
    };
}
