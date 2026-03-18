using Application.Contracts;
using Application.Services.GameInitialization;
using Domain.Factions;
using Domain.Game;
using Domain.Resources;
using Moq;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// GameInitializationService unit tests — resource initialization with faction bonuses.
/// </summary>
[Trait("Category", "Unit")]
public class GameInitializationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IKingdomRepository> _kingdomsMock = new();
    private readonly Mock<IFactionTypeRepository> _factionTypesMock = new();
    private readonly Mock<IKingdomResourceRepository> _kingdomResourcesMock = new();
    private readonly List<KingdomResource> _addedResources = [];
    private readonly GameInitializationService _sut;

    private static readonly Guid GameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid KingdomId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid KingdomId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid FactionId1 = Guid.Parse("f1111111-1111-1111-1111-111111111111");
    private static readonly Guid FactionId2 = Guid.Parse("f2222222-2222-2222-2222-222222222222");

    public GameInitializationServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Kingdoms).Returns(_kingdomsMock.Object);
        _unitOfWorkMock.Setup(u => u.FactionTypes).Returns(_factionTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.KingdomResources).Returns(_kingdomResourcesMock.Object);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        _kingdomResourcesMock.Setup(r => r.AddAsync(It.IsAny<KingdomResource>()))
            .Callback<KingdomResource>(kr => _addedResources.Add(kr))
            .ReturnsAsync((KingdomResource kr) => kr);

        _sut = new GameInitializationService(_unitOfWorkMock.Object);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Kingdom CreateKingdom(Guid kingdomId, Guid factionTypeId) => new()
    {
        Id = kingdomId,
        GameId = GameId,
        FactionTypeId = factionTypeId,
        Name = "Test Kingdom"
    };

    private static FactionType CreateFactionType(
        Guid id,
        EResourceType? bonusResource = null,
        int bonusAmount = 0) => new()
    {
        Id = id,
        StartingBonusResource = bonusResource,
        StartingBonusAmount = bonusAmount
    };

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeKingdomResources_CreatesBaseResourcesForEachKingdom()
    {
        // Arrange: 1 kingdom with no faction bonus
        var kingdom = CreateKingdom(KingdomId1, FactionId1);
        var faction = CreateFactionType(FactionId1);

        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId))
            .ReturnsAsync([kingdom]);
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId1))
            .ReturnsAsync(faction);

        // Act
        await _sut.InitializeKingdomResourcesAsync(GameId);

        // Assert: 5 resources created with base amounts
        _addedResources.Count.ShouldBe(5);

        _addedResources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(150);
        _addedResources.Single(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(60);
        _addedResources.Single(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(50);
        _addedResources.Single(r => r.ResourceType == EResourceType.Stone).Amount.ShouldBe(20);
        _addedResources.Single(r => r.ResourceType == EResourceType.Mana).Amount.ShouldBe(0);
    }

    [Fact]
    public async Task InitializeKingdomResources_AppliesFactionStartingBonus()
    {
        // Arrange: faction with Gold bonus +50
        var kingdom = CreateKingdom(KingdomId1, FactionId1);
        var faction = CreateFactionType(FactionId1, EResourceType.Gold, 50);

        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId))
            .ReturnsAsync([kingdom]);
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId1))
            .ReturnsAsync(faction);

        // Act
        await _sut.InitializeKingdomResourcesAsync(GameId);

        // Assert: Gold = 150 base + 50 bonus = 200, others unchanged
        _addedResources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(200);
        _addedResources.Single(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(60);
        _addedResources.Single(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(50);
        _addedResources.Single(r => r.ResourceType == EResourceType.Stone).Amount.ShouldBe(20);
        _addedResources.Single(r => r.ResourceType == EResourceType.Mana).Amount.ShouldBe(0);
    }

    [Fact]
    public async Task InitializeKingdomResources_HandlesMultipleKingdoms()
    {
        // Arrange: 2 kingdoms with different factions
        var kingdom1 = CreateKingdom(KingdomId1, FactionId1);
        var kingdom2 = CreateKingdom(KingdomId2, FactionId2);
        var faction1 = CreateFactionType(FactionId1, EResourceType.Gold, 50);
        var faction2 = CreateFactionType(FactionId2, EResourceType.Wood, 30);

        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId))
            .ReturnsAsync([kingdom1, kingdom2]);
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId1))
            .ReturnsAsync(faction1);
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId2))
            .ReturnsAsync(faction2);

        // Act
        await _sut.InitializeKingdomResourcesAsync(GameId);

        // Assert: 10 resources total (5 per kingdom)
        _addedResources.Count.ShouldBe(10);

        // Kingdom 1: Gold bonus
        var k1Resources = _addedResources.Where(r => r.KingdomId == KingdomId1).ToList();
        k1Resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(200);
        k1Resources.Single(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(50);

        // Kingdom 2: Wood bonus
        var k2Resources = _addedResources.Where(r => r.KingdomId == KingdomId2).ToList();
        k2Resources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(150);
        k2Resources.Single(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(80);
    }

    [Fact]
    public async Task InitializeKingdomResources_CommitsOnce()
    {
        // Arrange
        var kingdom = CreateKingdom(KingdomId1, FactionId1);
        var faction = CreateFactionType(FactionId1);

        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId))
            .ReturnsAsync([kingdom]);
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId1))
            .ReturnsAsync(faction);

        // Act
        await _sut.InitializeKingdomResourcesAsync(GameId);

        // Assert: CommitAsync called exactly once
        _unitOfWorkMock.Verify(u => u.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task InitializeKingdomResources_NoFactionBonus_UsesBaseAmountsOnly()
    {
        // Arrange: faction with StartingBonusResource=null and StartingBonusAmount=0
        var kingdom = CreateKingdom(KingdomId1, FactionId1);
        var faction = CreateFactionType(FactionId1, bonusResource: null, bonusAmount: 0);

        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId))
            .ReturnsAsync([kingdom]);
        _factionTypesMock.Setup(f => f.GetByIdAsync(FactionId1))
            .ReturnsAsync(faction);

        // Act
        await _sut.InitializeKingdomResourcesAsync(GameId);

        // Assert: all at base level
        _addedResources.Single(r => r.ResourceType == EResourceType.Gold).Amount.ShouldBe(150);
        _addedResources.Single(r => r.ResourceType == EResourceType.Food).Amount.ShouldBe(60);
        _addedResources.Single(r => r.ResourceType == EResourceType.Wood).Amount.ShouldBe(50);
        _addedResources.Single(r => r.ResourceType == EResourceType.Stone).Amount.ShouldBe(20);
        _addedResources.Single(r => r.ResourceType == EResourceType.Mana).Amount.ShouldBe(0);
    }
}
