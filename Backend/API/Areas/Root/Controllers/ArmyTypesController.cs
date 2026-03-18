using Domain.Military;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Areas.Root.Controllers;

public class ArmyTypesController : ReferenceDataBaseController<ArmyType>
{
    public ArmyTypesController(AppDbContext context) : base(context) { }

    protected override DbSet<ArmyType> DbSet => _context.ArmyTypes;

    protected override string EntityDisplayName => "Army Type";

    protected override void PopulateEntity(ArmyType entity, IFormCollection form)
    {
        entity.Attack = int.TryParse(form["Attack"].ToString(), out var atk) ? atk : 0;
        entity.HP = int.TryParse(form["HP"].ToString(), out var hp) ? hp : 0;
        entity.Initiative = int.TryParse(form["Initiative"].ToString(), out var init) ? init : 0;
        entity.DamageRangeMin = decimal.TryParse(form["DamageRangeMin"].ToString(), out var drMin) ? drMin : 0m;
        entity.DamageRangeMax = decimal.TryParse(form["DamageRangeMax"].ToString(), out var drMax) ? drMax : 0m;
        entity.ChipDamageRangeMin = decimal.TryParse(form["ChipDamageRangeMin"].ToString(), out var cdMin) ? cdMin : 0m;
        entity.ChipDamageRangeMax = decimal.TryParse(form["ChipDamageRangeMax"].ToString(), out var cdMax) ? cdMax : 0m;
        entity.TrainingCostGold = int.TryParse(form["TrainingCostGold"].ToString(), out var tcg) ? tcg : 0;
        entity.TrainingCostFood = int.TryParse(form["TrainingCostFood"].ToString(), out var tcf) ? tcf : 0;
        entity.TrainingCostStone = int.TryParse(form["TrainingCostStone"].ToString(), out var tcs) ? tcs : 0;
        entity.TrainingCostMana = int.TryParse(form["TrainingCostMana"].ToString(), out var tcm) ? tcm : 0;
        entity.UpkeepGold = int.TryParse(form["UpkeepGold"].ToString(), out var ug) ? ug : 0;
        entity.UpkeepFood = int.TryParse(form["UpkeepFood"].ToString(), out var uf) ? uf : 0;
        entity.UpkeepMana = int.TryParse(form["UpkeepMana"].ToString(), out var um) ? um : 0;
        entity.Description = form["Description"].ToString() is { Length: > 0 } desc ? desc : null;
        entity.IconUrl = form["IconUrl"].ToString() is { Length: > 0 } icon ? icon : null;

        var reqBldStr = form["RequiredBuildingTypeId"].ToString();
        entity.RequiredBuildingTypeId = Guid.TryParse(reqBldStr, out var reqBldId) ? reqBldId : Guid.Empty;
    }

    protected override object ToViewModel(ArmyType entity) => new
    {
        entity.Id,
        NameEn = entity.Name.Translate("en") ?? string.Empty,
        entity.Attack,
        entity.HP,
        entity.Initiative,
        entity.DamageRangeMin,
        entity.DamageRangeMax,
        entity.ChipDamageRangeMin,
        entity.ChipDamageRangeMax,
        entity.TrainingCostGold,
        entity.TrainingCostFood,
        entity.TrainingCostStone,
        entity.TrainingCostMana,
        entity.UpkeepGold,
        entity.UpkeepFood,
        entity.UpkeepMana,
        entity.Description,
        entity.IconUrl,
        entity.RequiredBuildingTypeId
    };
}
