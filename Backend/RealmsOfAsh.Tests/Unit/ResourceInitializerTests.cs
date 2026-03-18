using Domain.Resources;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

[Trait("Category", "Unit")]
public class ResourceInitializerTests
{
    private static readonly Guid KingdomId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void BaseStartingResources_HasCorrectValues()
    {
        ResourceInitializer.BaseStartingResources[EResourceType.Gold].ShouldBe(150);
        ResourceInitializer.BaseStartingResources[EResourceType.Food].ShouldBe(60);
        ResourceInitializer.BaseStartingResources[EResourceType.Wood].ShouldBe(50);
        ResourceInitializer.BaseStartingResources[EResourceType.Stone].ShouldBe(20);
        ResourceInitializer.BaseStartingResources[EResourceType.Mana].ShouldBe(0);
    }

    [Fact]
    public void CreateStartingResources_NoBonus_ReturnsBaseAmounts()
    {
        var resources = ResourceInitializer.CreateStartingResources(KingdomId, null, 0);

        resources.Count.ShouldBe(5);
        resources.First(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(150);
        resources.First(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(60);
        resources.First(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(50);
        resources.First(r => r.ResourceType == EResourceType.Stone).Amount.ShouldBe(20);
        resources.First(r => r.ResourceType == EResourceType.Mana).Amount.ShouldBe(0);
        resources.ShouldAllBe(r => r.KingdomId == KingdomId);
    }

    [Fact]
    public void CreateStartingResources_WithFactionBonus_AddsToMatchingResource()
    {
        var resources = ResourceInitializer.CreateStartingResources(KingdomId, EResourceType.Gold, 50);

        resources.First(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(200); // 150 + 50
        resources.First(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(60);   // unchanged
        resources.First(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(50);
        resources.First(r => r.ResourceType == EResourceType.Stone).Amount.ShouldBe(20);
        resources.First(r => r.ResourceType == EResourceType.Mana).Amount.ShouldBe(0);
    }

    [Fact]
    public void CreateStartingResources_EachEntityHasUniqueId()
    {
        var resources = ResourceInitializer.CreateStartingResources(KingdomId, null, 0);

        var ids = resources.Select(r => r.Id).ToHashSet();
        ids.Count.ShouldBe(5); // all unique
        ids.ShouldAllBe(id => id != Guid.Empty);
    }
}
