namespace Domain.Resources;

public static class ResourceInitializer
{
    /// <summary>
    /// Base starting resources for a new kingdom (before faction bonus).
    /// </summary>
    public static readonly Dictionary<EResourceType, int> BaseStartingResources = new()
    {
        { EResourceType.Gold, 150 },
        { EResourceType.Food, 60 },
        { EResourceType.Wood, 50 },
        { EResourceType.Stone, 20 },
        { EResourceType.Mana, 0 },
    };

    /// <summary>
    /// Creates 5 KingdomResource entities (one per EResourceType) with base amounts plus faction bonus.
    /// </summary>
    public static List<KingdomResource> CreateStartingResources(
        Guid kingdomId,
        EResourceType? bonusResource,
        int bonusAmount)
    {
        var now = DateTime.UtcNow;

        return BaseStartingResources.Select(kvp => new KingdomResource
        {
            Id = Guid.NewGuid(),
            KingdomId = kingdomId,
            ResourceType = kvp.Key,
            Amount = kvp.Value + (kvp.Key == bonusResource ? bonusAmount : 0),
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();
    }
}
