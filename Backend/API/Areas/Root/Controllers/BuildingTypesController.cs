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
        entity.CostGold = int.TryParse(form["CostGold"].ToString(), out var cg) ? cg : 0;
        entity.CostFood = int.TryParse(form["CostFood"].ToString(), out var cf) ? cf : 0;
        entity.CostWood = int.TryParse(form["CostWood"].ToString(), out var cw) ? cw : 0;
        entity.CostStone = int.TryParse(form["CostStone"].ToString(), out var cs) ? cs : 0;
        entity.CostMana = int.TryParse(form["CostMana"].ToString(), out var cm) ? cm : 0;
        entity.BaseYieldGold = int.TryParse(form["BaseYieldGold"].ToString(), out var yg) ? yg : 0;
        entity.BaseYieldFood = int.TryParse(form["BaseYieldFood"].ToString(), out var yf) ? yf : 0;
        entity.BaseYieldWood = int.TryParse(form["BaseYieldWood"].ToString(), out var yw) ? yw : 0;
        entity.BaseYieldStone = int.TryParse(form["BaseYieldStone"].ToString(), out var ys) ? ys : 0;
        entity.BaseYieldMana = int.TryParse(form["BaseYieldMana"].ToString(), out var ym) ? ym : 0;
        entity.ArmyCapacity = int.TryParse(form["ArmyCapacity"].ToString(), out var ac) ? ac : 0;
        if (form["DescriptionEn"].ToString() is { Length: > 0 } descEn)
            entity.Description.SetTranslation(descEn, "en");
        if (form["DescriptionEt"].ToString() is { Length: > 0 } descEt)
            entity.Description.SetTranslation(descEt, "et");
        entity.IconUrl = form["IconUrl"].ToString() is { Length: > 0 } icon ? icon : null;

        var prereqStr = form["UnlockedByBuildingTypeId"].ToString();
        entity.UnlockedByBuildingTypeId = Guid.TryParse(prereqStr, out var prereqId) ? prereqId : null;
    }

    protected override object ToViewModel(BuildingType entity) => new
    {
        entity.Id,
        NameEn = entity.Name.Translate("en") ?? string.Empty,
        NameEt = entity.Name.Translate("et") ?? string.Empty,
        entity.Tier,
        entity.Chain,
        entity.CostGold,
        entity.CostFood,
        entity.CostWood,
        entity.CostStone,
        entity.CostMana,
        entity.BaseYieldGold,
        entity.BaseYieldFood,
        entity.BaseYieldWood,
        entity.BaseYieldStone,
        entity.BaseYieldMana,
        entity.ArmyCapacity,
        DescriptionEn = entity.Description.Translate("en") ?? string.Empty,
        DescriptionEt = entity.Description.Translate("et") ?? string.Empty,
        entity.IconUrl,
        UnlockedByBuildingTypeId = entity.UnlockedByBuildingTypeId?.ToString() ?? string.Empty
    };
}
