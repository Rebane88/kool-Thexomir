using Domain.Buildings;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Areas.Root.Controllers;

public class BuildingTypesController : ReferenceDataBaseController<BuildingType>
{
    public BuildingTypesController(AppDbContext context) : base(context) { }

    protected override DbSet<BuildingType> DbSet => _context.BuildingTypes;

    protected override string EntityDisplayName => "Building Type";

    protected override void PopulateEntity(BuildingType entity, IFormCollection form)
    {
        entity.Tier = int.TryParse(form["Tier"].ToString(), out var tier) ? tier : 1;
        entity.Chain = form["Chain"].ToString();
        entity.GoldCost = int.TryParse(form["GoldCost"].ToString(), out var gc) ? gc : 0;
        entity.WoodCost = int.TryParse(form["WoodCost"].ToString(), out var wc) ? wc : 0;
        entity.StoneCost = int.TryParse(form["StoneCost"].ToString(), out var sc) ? sc : 0;
        entity.ManaCost = int.TryParse(form["ManaCost"].ToString(), out var mc) ? mc : 0;
        entity.FoodYield = int.TryParse(form["FoodYield"].ToString(), out var fy) ? fy : 0;
        entity.WoodYield = int.TryParse(form["WoodYield"].ToString(), out var wy) ? wy : 0;
        entity.StoneYield = int.TryParse(form["StoneYield"].ToString(), out var sy) ? sy : 0;
        entity.GoldYield = int.TryParse(form["GoldYield"].ToString(), out var gy) ? gy : 0;
        entity.ManaYield = int.TryParse(form["ManaYield"].ToString(), out var may) ? may : 0;
        entity.Description = form["Description"].ToString() is { Length: > 0 } desc ? desc : null;
        entity.IconUrl = form["IconUrl"].ToString() is { Length: > 0 } icon ? icon : null;

        var prereqStr = form["PrerequisiteBuildingTypeId"].ToString();
        entity.PrerequisiteBuildingTypeId = Guid.TryParse(prereqStr, out var prereqId) ? prereqId : null;
    }

    protected override object ToViewModel(BuildingType entity) => new
    {
        entity.Id,
        NameEn = entity.Name.Translate("en") ?? string.Empty,
        entity.Tier,
        entity.Chain,
        entity.GoldCost,
        entity.WoodCost,
        entity.StoneCost,
        entity.ManaCost,
        entity.FoodYield,
        entity.WoodYield,
        entity.StoneYield,
        entity.GoldYield,
        entity.ManaYield,
        entity.Description,
        entity.IconUrl,
        PrerequisiteBuildingTypeId = entity.PrerequisiteBuildingTypeId?.ToString() ?? string.Empty
    };
}
