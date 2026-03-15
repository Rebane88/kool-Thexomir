using Domain.Factions;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Areas.Root.Controllers;

public class FactionTypesController : ReferenceDataBaseController<FactionType>
{
    public FactionTypesController(AppDbContext context) : base(context) { }

    protected override DbSet<FactionType> DbSet => _context.FactionTypes;

    protected override string EntityDisplayName => "Faction Type";

    protected override void PopulateEntity(FactionType entity, IFormCollection form)
    {
        entity.BuildingCostModifier = decimal.TryParse(form["BuildingCostModifier"].ToString(), out var bcm) ? bcm : 1m;
        entity.StartingGold = int.TryParse(form["StartingGold"].ToString(), out var gold) ? gold : 0;
        entity.StartingFood = int.TryParse(form["StartingFood"].ToString(), out var food) ? food : 0;
        entity.StartingWood = int.TryParse(form["StartingWood"].ToString(), out var wood) ? wood : 0;
        entity.StartingStone = int.TryParse(form["StartingStone"].ToString(), out var stone) ? stone : 0;
        entity.StartingMana = int.TryParse(form["StartingMana"].ToString(), out var mana) ? mana : 0;
        entity.Description = form["Description"].ToString() is { Length: > 0 } desc ? desc : null;
    }

    protected override object ToViewModel(FactionType entity) => new
    {
        entity.Id,
        NameEn = entity.Name.Translate("en") ?? string.Empty,
        entity.BuildingCostModifier,
        entity.StartingGold,
        entity.StartingFood,
        entity.StartingWood,
        entity.StartingStone,
        entity.StartingMana,
        entity.Description
    };
}
