using Application.Contracts;
using Application.Services.Combat;
using Application.Services.Combat.DTOs;
using Base;
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
    private readonly Mock<IGameRepository> _gamesMock = new();
    private readonly Mock<IKingdomRepository> _kingdomsMock = new();
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
    private static readonly Guid AttackId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Army1Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Army2Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid Army3Id = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid ArmyTypeId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid EnemyUserId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    public CombatServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Games).Returns(_gamesMock.Object);
        _unitOfWorkMock.Setup(u => u.Kingdoms).Returns(_kingdomsMock.Object);
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

    private static Game CreateGame(EGamePhase phase = EGamePhase.Action) => new()
    {
        Id = GameId,
        Status = EGameStatus.InProgress,
        RoundNumber = 3,
        CurrentPhase = phase,
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

    private static Kingdom CreateEnemyKingdom() => new()
    {
        Id = EnemyKingdomId,
        GameId = GameId,
        AppUserId = EnemyUserId,
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

    private static DeclaredAttack CreateDeclaredAttack() => new()
    {
        Id = AttackId,
        GameId = GameId,
        RoundNumber = 3,
        AttackerKingdomId = KingdomId,
        DefenderKingdomId = EnemyKingdomId,
        TargetTileId = TargetTileId,
        RiskedTileId = RiskedTileId
    };

    private static ArmyType CreateArmyType() => new()
    {
        Id = ArmyTypeId,
        Name = new LangStr("Swordsman", "en"),
        Attack = 50,
        HP = 100,
        Initiative = 30,
        DamageRangeMin = 0.8m,
        DamageRangeMax = 1.2m,
        ChipDamageRangeMin = 0.1m,
        ChipDamageRangeMax = 0.3m
    };

    private static List<Domain.Military.Army> CreateArmies() =>
    [
        new() { Id = Army1Id, KingdomId = KingdomId, ArmyTypeId = ArmyTypeId, CurrentHP = 100, MaxHP = 100, ArmyType = CreateArmyType() },
        new() { Id = Army2Id, KingdomId = KingdomId, ArmyTypeId = ArmyTypeId, CurrentHP = 80, MaxHP = 100, ArmyType = CreateArmyType() },
        new() { Id = Army3Id, KingdomId = KingdomId, ArmyTypeId = ArmyTypeId, CurrentHP = 60, MaxHP = 100, ArmyType = CreateArmyType() }
    ];

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

    private void SetupBattlePhaseForSelection()
    {
        var game = CreateGame(EGamePhase.Battle);
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomByUserAndGameAsync(UserId, GameId)).ReturnsAsync(CreateKingdom());
        _declaredAttacksMock.Setup(d => d.GetByIdAsync(AttackId)).ReturnsAsync(CreateDeclaredAttack());
        _tilesMock.Setup(x => x.GetByIdAsync(TargetTileId)).ReturnsAsync(CreateTargetTile());
        _tilesMock.Setup(x => x.GetByIdAsync(RiskedTileId)).ReturnsAsync(CreateRiskedTile());
        _armiesMock.Setup(x => x.GetArmiesForKingdomAsync(KingdomId)).ReturnsAsync(CreateArmies());
        _declaredAttacksMock.Setup(d => d.UpdateAsync(It.IsAny<DeclaredAttack>()))
            .ReturnsAsync((DeclaredAttack da) => da);
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

    // -------------------------------------------------------------------------
    // SelectArmiesAsync tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SelectArmiesAsync_ValidSelection_ReturnsSuccess()
    {
        SetupBattlePhaseForSelection();

        var request = new SelectArmiesRequest
        {
            DeclaredAttackId = AttackId,
            ArmyIds = [Army1Id, Army2Id]
        };

        var result = await _sut.SelectArmiesAsync(GameId, UserId, request);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.DeclaredAttackId.ShouldBe(AttackId);
        result.Value.KingdomId.ShouldBe(KingdomId);
        result.Value.ArmiesSelected.ShouldBe(2);
        result.Value.MaxArmies.ShouldBe(3); // non-castle = 3
        _unitOfWorkMock.Verify(u => u.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task SelectArmiesAsync_NotInBattle_ReturnsError()
    {
        var game = CreateGame(EGamePhase.Battle);
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);

        // Player is in game but not involved in this attack
        var uninvolvedKingdom = new Kingdom
        {
            Id = Guid.NewGuid(),
            GameId = GameId,
            AppUserId = UserId,
            FactionTypeId = FactionTypeId,
            Status = EKingdomStatus.Active
        };
        _kingdomsMock.Setup(k => k.GetKingdomByUserAndGameAsync(UserId, GameId)).ReturnsAsync(uninvolvedKingdom);
        _declaredAttacksMock.Setup(d => d.GetByIdAsync(AttackId)).ReturnsAsync(CreateDeclaredAttack());

        var request = new SelectArmiesRequest
        {
            DeclaredAttackId = AttackId,
            ArmyIds = [Army1Id]
        };

        var result = await _sut.SelectArmiesAsync(GameId, UserId, request);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("not involved");
    }

    [Fact]
    public async Task SelectArmiesAsync_WrongPhase_ReturnsError()
    {
        var game = CreateGame(EGamePhase.Action);
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);

        var request = new SelectArmiesRequest
        {
            DeclaredAttackId = AttackId,
            ArmyIds = [Army1Id]
        };

        var result = await _sut.SelectArmiesAsync(GameId, UserId, request);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Battle Phase");
    }

    [Fact]
    public async Task SelectArmiesAsync_TooManyArmies_ReturnsError()
    {
        SetupBattlePhaseForSelection();

        // Non-castle allows max 3, try 4
        var army4Id = Guid.NewGuid();
        var armies = CreateArmies();
        armies.Add(new Domain.Military.Army { Id = army4Id, KingdomId = KingdomId, ArmyTypeId = ArmyTypeId, CurrentHP = 50, MaxHP = 100, ArmyType = CreateArmyType() });
        _armiesMock.Setup(x => x.GetArmiesForKingdomAsync(KingdomId)).ReturnsAsync(armies);

        var request = new SelectArmiesRequest
        {
            DeclaredAttackId = AttackId,
            ArmyIds = [Army1Id, Army2Id, Army3Id, army4Id]
        };

        var result = await _sut.SelectArmiesAsync(GameId, UserId, request);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("Cannot select more than 3");
    }

    // -------------------------------------------------------------------------
    // GetArmyRevealAsync tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetArmyRevealAsync_ValidRequest_ReturnsReveal()
    {
        var game = CreateGame(EGamePhase.Battle);
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomByUserAndGameAsync(UserId, GameId)).ReturnsAsync(CreateKingdom());

        var attack = CreateDeclaredAttack();
        attack.AttackerSelectedArmyIds = $"{Army1Id},{Army2Id}";
        attack.DefenderSelectedArmyIds = $"{Army3Id}";
        _declaredAttacksMock.Setup(d => d.GetByIdAsync(AttackId)).ReturnsAsync(attack);

        var attackerArmies = new List<Domain.Military.Army>
        {
            new() { Id = Army1Id, KingdomId = KingdomId, ArmyTypeId = ArmyTypeId, CurrentHP = 100, MaxHP = 100, ArmyType = CreateArmyType() },
            new() { Id = Army2Id, KingdomId = KingdomId, ArmyTypeId = ArmyTypeId, CurrentHP = 80, MaxHP = 100, ArmyType = CreateArmyType() }
        };
        _armiesMock.Setup(a => a.GetArmiesWithTypeForKingdomAsync(KingdomId))
            .ReturnsAsync(attackerArmies);

        var defenderArmies = new List<Domain.Military.Army>
        {
            new() { Id = Army3Id, KingdomId = EnemyKingdomId, ArmyTypeId = ArmyTypeId, CurrentHP = 60, MaxHP = 100, ArmyType = CreateArmyType() }
        };
        _armiesMock.Setup(a => a.GetArmiesWithTypeForKingdomAsync(EnemyKingdomId))
            .ReturnsAsync(defenderArmies);

        var result = await _sut.GetArmyRevealAsync(GameId, UserId, AttackId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.DeclaredAttackId.ShouldBe(AttackId);
        result.Value.AttackerKingdomId.ShouldBe(KingdomId);
        result.Value.DefenderKingdomId.ShouldBe(EnemyKingdomId);
        result.Value.AttackerArmies.Count.ShouldBe(2);
        result.Value.DefenderArmies.Count.ShouldBe(1);
        result.Value.AttackerArmies[0].ArmyTypeName.ShouldBe("Swordsman");
        result.Value.AttackerArmies[0].CurrentHP.ShouldBe(100);
        result.Value.DefenderArmies[0].CurrentHP.ShouldBe(60);
    }

    // -------------------------------------------------------------------------
    // SetLineupAsync tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SetLineupAsync_ValidReorder_ReturnsSuccess()
    {
        var game = CreateGame(EGamePhase.Battle);
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomByUserAndGameAsync(UserId, GameId)).ReturnsAsync(CreateKingdom());

        var attack = CreateDeclaredAttack();
        attack.AttackerSelectedArmyIds = $"{Army1Id},{Army2Id},{Army3Id}";
        _declaredAttacksMock.Setup(d => d.GetByIdAsync(AttackId)).ReturnsAsync(attack);
        _declaredAttacksMock.Setup(d => d.UpdateAsync(It.IsAny<DeclaredAttack>()))
            .ReturnsAsync((DeclaredAttack da) => da);

        // Reorder: reverse the lineup
        var request = new SetLineupRequest
        {
            DeclaredAttackId = AttackId,
            ArmyIdsInOrder = [Army3Id, Army2Id, Army1Id]
        };

        var result = await _sut.SetLineupAsync(GameId, UserId, request);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ArmiesSelected.ShouldBe(3);
        _unitOfWorkMock.Verify(u => u.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task SetLineupAsync_MismatchedArmies_ReturnsError()
    {
        var game = CreateGame(EGamePhase.Battle);
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomByUserAndGameAsync(UserId, GameId)).ReturnsAsync(CreateKingdom());

        var attack = CreateDeclaredAttack();
        attack.AttackerSelectedArmyIds = $"{Army1Id},{Army2Id}";
        _declaredAttacksMock.Setup(d => d.GetByIdAsync(AttackId)).ReturnsAsync(attack);

        // Try to set lineup with different army IDs
        var request = new SetLineupRequest
        {
            DeclaredAttackId = AttackId,
            ArmyIdsInOrder = [Army1Id, Army3Id] // Army3Id was not selected
        };

        var result = await _sut.SetLineupAsync(GameId, UserId, request);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("same armies");
    }

    [Fact]
    public async Task SetLineupAsync_NoSelectionYet_ReturnsError()
    {
        var game = CreateGame(EGamePhase.Battle);
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomByUserAndGameAsync(UserId, GameId)).ReturnsAsync(CreateKingdom());

        var attack = CreateDeclaredAttack();
        // No selection stored (AttackerSelectedArmyIds is null)
        _declaredAttacksMock.Setup(d => d.GetByIdAsync(AttackId)).ReturnsAsync(attack);

        var request = new SetLineupRequest
        {
            DeclaredAttackId = AttackId,
            ArmyIdsInOrder = [Army1Id, Army2Id]
        };

        var result = await _sut.SetLineupAsync(GameId, UserId, request);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("select armies");
    }
}
