using Domain.Factions;
using Domain.Resources;
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
        entity.AttackModifier = decimal.TryParse(form["AttackModifier"].ToString(), out var am) ? am : 1.0m;
        entity.HPModifier = decimal.TryParse(form["HPModifier"].ToString(), out var hm) ? hm : 1.0m;
        entity.InitiativeModifier = decimal.TryParse(form["InitiativeModifier"].ToString(), out var im) ? im : 1.0m;
        entity.ChipDamageModifier = decimal.TryParse(form["ChipDamageModifier"].ToString(), out var cdm) ? cdm : 1.0m;
        entity.ResourceProductionModifier = decimal.TryParse(form["ResourceProductionModifier"].ToString(), out var rpm) ? rpm : 1.0m;
        entity.BuildingCostModifier = decimal.TryParse(form["BuildingCostModifier"].ToString(), out var bcm) ? bcm : 1.0m;
        entity.TrainingCostModifier = decimal.TryParse(form["TrainingCostModifier"].ToString(), out var tcm) ? tcm : 1.0m;
        entity.ActionPointModifier = int.TryParse(form["ActionPointModifier"].ToString(), out var apm) ? apm : 0;
        entity.HealRateModifier = decimal.TryParse(form["HealRateModifier"].ToString(), out var hrm) ? hrm : 1.0m;
        entity.StartingBonusResource = Enum.TryParse<EResourceType>(
            form["StartingBonusResource"].ToString(), out var sbr) ? sbr : null;
        entity.StartingBonusAmount = int.TryParse(form["StartingBonusAmount"].ToString(), out var sba) ? sba : 0;
        if (form["DescriptionEn"].ToString() is { Length: > 0 } descEn)
            entity.Description.SetTranslation(descEn, "en");
        if (form["DescriptionEt"].ToString() is { Length: > 0 } descEt)
            entity.Description.SetTranslation(descEt, "et");
        if (form["LoreEn"].ToString() is { Length: > 0 } loreEn)
            entity.Lore.SetTranslation(loreEn, "en");
        if (form["LoreEt"].ToString() is { Length: > 0 } loreEt)
            entity.Lore.SetTranslation(loreEt, "et");
    }

    protected override object ToViewModel(FactionType entity) => new
    {
        entity.Id,
        NameEn = entity.Name.Translate("en") ?? string.Empty,
        NameEt = entity.Name.Translate("et") ?? string.Empty,
        entity.AttackModifier,
        entity.HPModifier,
        entity.InitiativeModifier,
        entity.ChipDamageModifier,
        entity.ResourceProductionModifier,
        entity.BuildingCostModifier,
        entity.TrainingCostModifier,
        entity.ActionPointModifier,
        entity.HealRateModifier,
        entity.StartingBonusResource,
        entity.StartingBonusAmount,
        DescriptionEn = entity.Description.Translate("en") ?? string.Empty,
        DescriptionEt = entity.Description.Translate("et") ?? string.Empty,
        LoreEn = entity.Lore.Translate("en") ?? string.Empty,
        LoreEt = entity.Lore.Translate("et") ?? string.Empty
    };
}
