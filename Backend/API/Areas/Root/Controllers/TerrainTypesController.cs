using System.Globalization;
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
        entity.IconUrl = form["IconUrl"].ToString() is { Length: > 0 } icon ? icon : null;
    }

    protected override object ToViewModel(TerrainType entity) => new
    {
        entity.Id,
        NameEn = entity.Name.GetValueOrDefault("en", string.Empty),
        NameEt = entity.Name.GetValueOrDefault("et", string.Empty),
        ResourceMultiplier = entity.ResourceMultiplier.ToString(CultureInfo.InvariantCulture),
        entity.ResourceBonusType,
        entity.MapColor,
        entity.IconUrl
    };
}
