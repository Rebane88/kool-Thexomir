using Base;
using Base.Contracts;
using Domain.Factions;
using Domain.Resources;

namespace Infrastructure.Seeding.Seeders;

public class FactionTypeSeeder : ISeeder
{
    public int Order => 5;

    public static readonly Guid IronThroneId = new("EEEEEEEE-0001-0000-0000-000000000001");
    public static readonly Guid MageCouncilId = new("EEEEEEEE-0001-0000-0000-000000000002");
    public static readonly Guid MerchantRepublicId = new("EEEEEEEE-0001-0000-0000-000000000003");
    public static readonly Guid ForestElvesId = new("EEEEEEEE-0001-0000-0000-000000000004");

    private static LangStr L(string en, string et)
    {
        var ls = new LangStr(en, "en");
        ls.SetTranslation(et, "et");
        return ls;
    }

    public void Seed(object context)
    {
        var db = (AppDbContext)context;
        if (db.FactionTypes.Any()) return;

        var now = DateTime.UtcNow;

        db.FactionTypes.AddRange(
            new FactionType
            {
                Id = IronThroneId,
                Name = L("Iron Throne", "Raudtroon"),
                Description = L(
                    "Masters of war. Superior attack power and chip damage, but reduced resource production.",
                    "Sõjameistrid. Suurepärane rünnakujõud ja lisakahju, kuid vähendatud ressursitootmine."),
                Lore = L(
                    "Forged in centuries of conquest, the Iron Throne rules through military supremacy.",
                    "Sajandite vallutustes sepistatud Raudtroon valitseb sõjalise ülemvõimu kaudu."),
                AttackModifier = 1.15m,
                HPModifier = 1.0m,
                InitiativeModifier = 1.0m,
                ChipDamageModifier = 1.50m,
                ResourceProductionModifier = 0.85m,
                BuildingCostModifier = 1.0m,
                TrainingCostModifier = 1.0m,
                ActionPointModifier = 0,
                HealRateModifier = 1.0m,
                StartingBonusResource = EResourceType.Gold,
                StartingBonusAmount = 50,
                CreatedAt = now, UpdatedAt = now
            },
            new FactionType
            {
                Id = MageCouncilId,
                Name = L("Mage Council", "Maaginõukogu"),
                Description = L(
                    "Arcane masters. Higher initiative and an extra action point, but reduced army HP.",
                    "Müstilised meistrid. Kõrgem initsiatiiv ja lisategevuspunkt, kuid vähendatud väe elupunktid."),
                Lore = L(
                    "The Mage Council channels ancient arcane knowledge to dominate through superior tactics.",
                    "Maaginõukogu kasutab iidset müstilist tarkust, et domineerida ülemate taktikate kaudu."),
                AttackModifier = 1.0m,
                HPModifier = 0.85m,
                InitiativeModifier = 1.20m,
                ChipDamageModifier = 1.0m,
                ResourceProductionModifier = 1.0m,
                BuildingCostModifier = 1.0m,
                TrainingCostModifier = 1.0m,
                ActionPointModifier = 1,
                HealRateModifier = 1.0m,
                StartingBonusResource = EResourceType.Mana,
                StartingBonusAmount = 30,
                CreatedAt = now, UpdatedAt = now
            },
            new FactionType
            {
                Id = MerchantRepublicId,
                Name = L("Merchant Republic", "Kaupmehevabariik"),
                Description = L(
                    "Economic powerhouse. Cheaper buildings and training, but weaker in combat.",
                    "Majanduslik jõujaam. Odavamad ehitised ja treening, kuid nõrgem lahingus."),
                Lore = L(
                    "Trade routes spanning the known world fuel the Merchant Republic's endless coffers.",
                    "Tuntud maailma hõlmavad kaubateed toidavad Kaupmehevabariigi lõputuid rahakotte."),
                AttackModifier = 0.90m,
                HPModifier = 1.0m,
                InitiativeModifier = 1.0m,
                ChipDamageModifier = 1.0m,
                ResourceProductionModifier = 1.0m,
                BuildingCostModifier = 0.80m,
                TrainingCostModifier = 0.85m,
                ActionPointModifier = 0,
                HealRateModifier = 1.0m,
                StartingBonusResource = EResourceType.Gold,
                StartingBonusAmount = 100,
                CreatedAt = now, UpdatedAt = now
            },
            new FactionType
            {
                Id = ForestElvesId,
                Name = L("Forest Elves", "Metsahaldjad"),
                Description = L(
                    "Resilient defenders. Higher HP and healing rate, but one fewer action point.",
                    "Vastupidavad kaitsjad. Kõrgemad elupunktid ja tervenemiskiirus, kuid üks tegevuspunkt vähem."),
                Lore = L(
                    "Ancient guardians of the deep forests, the Elves endure through patience and regeneration.",
                    "Sügavate metsade iidsed kaitsjad, haldjad kestavad kannatuse ja taastumise kaudu."),
                AttackModifier = 1.0m,
                HPModifier = 1.15m,
                InitiativeModifier = 1.0m,
                ChipDamageModifier = 1.0m,
                ResourceProductionModifier = 1.0m,
                BuildingCostModifier = 1.0m,
                TrainingCostModifier = 1.0m,
                ActionPointModifier = -1,
                HealRateModifier = 1.50m,
                StartingBonusResource = EResourceType.Wood,
                StartingBonusAmount = 50,
                CreatedAt = now, UpdatedAt = now
            }
        );

        db.SaveChanges();
    }
}
