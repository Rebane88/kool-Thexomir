using Application.Contracts;
using Application.Services.Combat;
using Application.Services.Combat.DTOs;
using Base.Contracts;
using Domain.Game;
using Domain.Map;
using Domain.Military;
using Moq;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

[Trait("Category", "Unit")]
public class CombatServiceTests
{
    // Mocks
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IGameGuard> _gameGuardMock = new();
    private readonly Mock<ITileRepository> _tilesMock = new();
    private readonly Mock<IArmyRepository> _armiesMock = new();
    private readonly Mock<IDeclaredAttackRepository> _declaredAttacksMock = new();
    private readonly Mock<ITurnLogRepository> _turnLogsMock = new();
    private readonly CombatService _sut;

    // Fixed IDs
    private static readonly Guid GameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid KingdomId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid EnemyKingdomId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid TargetTileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RiskedTileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid FactionTypeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    public CombatServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Tiles).Returns(_tilesMock.Object);
        _unitOfWorkMock.Setup(u => u.Armies).Returns(_armiesMock.Object);
        _unitOfWorkMock.Setup(u => u.DeclaredAttacks).Returns(_declaredAttacksMock.Object);
        _unitOfWorkMock.Setup(u => u.TurnLogs).Returns(_turnLogsMock.Object);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        _sut = new CombatService(_unitOfWorkMock.Object, _gameGuardMock.Object);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Game CreateGame() => new()
    {
        Id = GameId,
        Status = EGameStatus.InProgress,
        RoundNumber = 3,
        CurrentPhase = EGamePhase.Action,
        CurrentTurnKingdomId = KingdomId,
        RemainingActionPoints = 3
    };

    private static Kingdom CreateKingdom() => new()
    {
        Id = KingdomId,
        GameId = GameId,
        AppUserId = UserId,
        FactionTypeId = FactionTypeId,
        Status = EKingdomStatus.Active
    };

    private static Tile CreateTargetTile() => new()
    {
        Id = TargetTileId,
        GameId = GameId,
        CoordQ = 1,
        CoordR = 0,
        KingdomId = EnemyKingdomId,
        IsCastle = false
    };

    private static Tile CreateRiskedTile() => new()
    {
        Id = RiskedTileId,
        GameId = GameId,
        CoordQ = 0,
        CoordR = 0,
        KingdomId = KingdomId,
        IsCastle = false
    };

    private static List<Tile> CreateAttackerTiles() =>
    [
        new() { Id = RiskedTileId, CoordQ = 0, CoordR = 0, KingdomId = KingdomId },
        new() { Id = Guid.NewGuid(), CoordQ = -1, CoordR = 0, KingdomId = KingdomId }
    ];

    private static DeclareAttackRequest CreateRequest() => new()
    {
        TargetTileId = TargetTileId,
        RiskedTileId = RiskedTileId
    };

    private void SetupSuccessfulGuard()
    {
        var game = CreateGame();
        var kingdom = CreateKingdom();
        _gameGuardMock.Setup(g => g.ValidateActionAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Ok(new GameGuardContext(game, kingdom)));
    }

    private void SetupFullSuccessPath()
    {
        SetupSuccessfulGuard();

        _tilesMock.Setup(x => x.GetByIdAsync(TargetTileId)).ReturnsAsync(CreateTargetTile());
        _tilesMock.Setup(x => x.GetByIdAsync(RiskedTileId)).ReturnsAsync(CreateRiskedTile());
        _tilesMock.Setup(x => x.GetTilesForKingdomAsync(KingdomId)).ReturnsAsync(CreateAttackerTiles());

        var armies = new List<Domain.Military.Army>
        {
            new() { Id = Guid.NewGuid(), KingdomId = KingdomId, CurrentHP = 100, MaxHP = 100 }
        };
        _armiesMock.Setup(x => x.GetArmiesForKingdomAsync(KingdomId))
            .ReturnsAsync(armies);

        _declaredAttacksMock.Setup(x => x.GetLockedTileIdsForGameRoundAsync(GameId, 3))
            .ReturnsAsync(new HashSet<Guid>());

        _declaredAttacksMock.Setup(x => x.AddAsync(It.IsAny<DeclaredAttack>()))
            .ReturnsAsync((DeclaredAttack da) => da);

        _turnLogsMock.Setup(x => x.AddAsync(It.IsAny<TurnLog>()))
            .ReturnsAsync((TurnLog tl) => tl);
    }

    // -------------------------------------------------------------------------
    // DeclareAttackAsync tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeclareAttackAsync_ValidAttack_ReturnsSuccess()
    {
        SetupFullSuccessPath();

        var result = await _sut.DeclareAttackAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value!.TargetTileId.ShouldBe(TargetTileId);
        result.Value.RiskedTileId.ShouldBe(RiskedTileId);
        result.Value.AttackerKingdomId.ShouldBe(KingdomId);
        result.Value.DefenderKingdomId.ShouldBe(EnemyKingdomId);

        _declaredAttacksMock.Verify(x => x.AddAsync(It.Is<DeclaredAttack>(
            da => da.TargetTileId == TargetTileId && da.RiskedTileId == RiskedTileId)), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeclareAttackAsync_InvalidTarget_ReturnsError()
    {
        SetupSuccessfulGuard();

        // Target tile not found
        _tilesMock.Setup(x => x.GetByIdAsync(TargetTileId)).ReturnsAsync((Tile?)null);

        var result = await _sut.DeclareAttackAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Target tile not found.");
    }

    [Fact]
    public async Task DeclareAttackAsync_NoAP_ReturnsError()
    {
        _gameGuardMock.Setup(g => g.ValidateActionAsync(GameId, UserId))
            .ReturnsAsync(Result<GameGuardContext>.Fail("No action points remaining."));

        var result = await _sut.DeclareAttackAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("action points");
    }

    [Fact]
    public async Task DeclareAttackAsync_LockedTile_ReturnsError()
    {
        SetupSuccessfulGuard();

        _tilesMock.Setup(x => x.GetByIdAsync(TargetTileId)).ReturnsAsync(CreateTargetTile());
        _tilesMock.Setup(x => x.GetByIdAsync(RiskedTileId)).ReturnsAsync(CreateRiskedTile());
        _tilesMock.Setup(x => x.GetTilesForKingdomAsync(KingdomId)).ReturnsAsync(CreateAttackerTiles());
        _armiesMock.Setup(x => x.GetArmiesForKingdomAsync(KingdomId))
            .ReturnsAsync(new List<Domain.Military.Army> { new() { Id = Guid.NewGuid() } });

        // Target tile is already locked
        _declaredAttacksMock.Setup(x => x.GetLockedTileIdsForGameRoundAsync(GameId, 3))
            .ReturnsAsync(new HashSet<Guid> { TargetTileId });

        var result = await _sut.DeclareAttackAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("locked");
    }

    [Fact]
    public async Task DeclareAttackAsync_NoArmies_ReturnsError()
    {
        SetupSuccessfulGuard();

        _tilesMock.Setup(x => x.GetByIdAsync(TargetTileId)).ReturnsAsync(CreateTargetTile());
        _tilesMock.Setup(x => x.GetByIdAsync(RiskedTileId)).ReturnsAsync(CreateRiskedTile());
        _tilesMock.Setup(x => x.GetTilesForKingdomAsync(KingdomId)).ReturnsAsync(CreateAttackerTiles());

        // No armies
        _armiesMock.Setup(x => x.GetArmiesForKingdomAsync(KingdomId))
            .ReturnsAsync(new List<Domain.Military.Army>());

        _declaredAttacksMock.Setup(x => x.GetLockedTileIdsForGameRoundAsync(GameId, 3))
            .ReturnsAsync(new HashSet<Guid>());

        var result = await _sut.DeclareAttackAsync(GameId, UserId, CreateRequest());

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("no armies");
    }
}
