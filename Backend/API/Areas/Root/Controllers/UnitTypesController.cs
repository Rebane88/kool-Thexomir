using Domain.Military;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Areas.Root.Controllers;

public class UnitTypesController : ReferenceDataBaseController<UnitType>
{
    public UnitTypesController(AppDbContext context) : base(context) { }

    protected override DbSet<UnitType> DbSet => _context.UnitTypes;

    protected override string EntityDisplayName => "Unit Type";

    protected override void PopulateEntity(UnitType entity, IFormCollection form)
    {
        entity.BaseStrength = int.TryParse(form["BaseStrength"].ToString(), out var bs) ? bs : 0;
        entity.GoldCost = int.TryParse(form["GoldCost"].ToString(), out var gc) ? gc : 0;
        entity.FoodCost = int.TryParse(form["FoodCost"].ToString(), out var fc) ? fc : 0;
        entity.WoodCost = int.TryParse(form["WoodCost"].ToString(), out var wc) ? wc : 0;
        entity.StoneCost = int.TryParse(form["StoneCost"].ToString(), out var sc) ? sc : 0;
        entity.ManaCost = int.TryParse(form["ManaCost"].ToString(), out var mac) ? mac : 0;
        entity.Upkeep = int.TryParse(form["Upkeep"].ToString(), out var upk) ? upk : 0;
        entity.Description = form["Description"].ToString() is { Length: > 0 } desc ? desc : null;
        entity.IconUrl = form["IconUrl"].ToString() is { Length: > 0 } icon ? icon : null;
    }

    protected override object ToViewModel(UnitType entity) => new
    {
        entity.Id,
        NameEn = entity.Name.Translate("en") ?? string.Empty,
        entity.BaseStrength,
        entity.GoldCost,
        entity.FoodCost,
        entity.WoodCost,
        entity.StoneCost,
        entity.ManaCost,
        entity.Upkeep,
        entity.Description,
        entity.IconUrl
    };
}
