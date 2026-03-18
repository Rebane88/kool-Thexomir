using Application.Contracts;
using Application.Services.Lobby;
using Application.Services.Lobby.DTOs;
using Base;
using Base.Contracts;
using Domain.Factions;
using Domain.Game;
using Moq;
using Shouldly;

namespace RealmsOfAsh.Tests.Unit;

/// <summary>
/// LobbyService unit tests — fully mocked, no database dependency.
/// Covers all lifecycle transitions: create, join, leave, select faction, start game, get lobby.
/// Tests every guard clause, invalid state, and edge case.
/// </summary>
public class LobbyServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IIdentityService> _identityMock = new();
    private readonly Mock<IGameRepository> _gamesMock = new();
    private readonly Mock<IKingdomRepository> _kingdomsMock = new();
    private readonly Mock<IFactionTypeRepository> _factionTypesMock = new();
    private readonly LobbyService _sut;

    public LobbyServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Games).Returns(_gamesMock.Object);
        _unitOfWorkMock.Setup(u => u.Kingdoms).Returns(_kingdomsMock.Object);
        _unitOfWorkMock.Setup(u => u.FactionTypes).Returns(_factionTypesMock.Object);
        _unitOfWorkMock.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);
        _identityMock.Setup(i => i.GetEmailsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string>());

        _sut = new LobbyService(_unitOfWorkMock.Object, _identityMock.Object);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static readonly Guid UserId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserId3 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid GameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid FactionId1 = Guid.Parse("f1111111-1111-1111-1111-111111111111");
    private static readonly Guid FactionId2 = Guid.Parse("f2222222-2222-2222-2222-222222222222");

    private static Game MakeLobbyGame(int maxPlayers = 4, EGameStatus status = EGameStatus.Lobby) => new()
    {
        Id = GameId,
        Status = status,
        MaxPlayers = maxPlayers,
        WinCondition = EWinCondition.Elimination,
        LobbyCode = "ABCDEF",
        HostUserId = UserId1,
        Kingdoms = new List<Kingdom>()
    };

    private static Kingdom MakeKingdom(Guid userId, Guid gameId, Guid? factionTypeId = null,
        DateTime? createdAt = null, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        GameId = gameId,
        AppUserId = userId,
        FactionTypeId = factionTypeId ?? Guid.Empty,
        Name = string.Empty,
        CreatedAt = createdAt ?? DateTime.UtcNow
    };

    private static FactionType MakeFaction(Guid id, string name = "Test Faction") => new()
    {
        Id = id,
        Name = new LangStr(name, "en")
    };

    // -------------------------------------------------------------------------
    // Create lobby tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateLobby_ReturnsOk_WithInviteCode()
    {
        _gamesMock.Setup(g => g.ExistsByLobbyCodeAsync(It.IsAny<string>())).ReturnsAsync(false);
        _gamesMock.Setup(g => g.AddAsync(It.IsAny<Game>())).ReturnsAsync((Game g) => g);
        _kingdomsMock.Setup(k => k.AddAsync(It.IsAny<Kingdom>())).ReturnsAsync((Kingdom k) => k);

        var result = await _sut.CreateLobbyAsync(UserId1, new CreateLobbyRequest
        {
            MaxPlayers = 4,
            WinCondition = EWinCondition.Elimination
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.LobbyId.ShouldNotBe(Guid.Empty);
        result.Value.InviteCode.ShouldNotBeNullOrEmpty();
        result.Value.InviteCode.Length.ShouldBe(6);
    }

    [Fact]
    public async Task CreateLobby_InvalidMaxPlayers_Zero_ReturnsFail()
    {
        var result = await _sut.CreateLobbyAsync(UserId1, new CreateLobbyRequest { MaxPlayers = 0 });

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateLobby_InvalidMaxPlayers_Nine_ReturnsFail()
    {
        var result = await _sut.CreateLobbyAsync(UserId1, new CreateLobbyRequest { MaxPlayers = 9 });

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
    }

    // -------------------------------------------------------------------------
    // Join lobby tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task JoinLobby_ValidCode_ReturnsOk()
    {
        var game = MakeLobbyGame(maxPlayers: 4);
        var kingdoms = new List<Kingdom> { MakeKingdom(UserId1, GameId) };

        _gamesMock.Setup(g => g.GetByLobbyCodeAsync("ABCDEF")).ReturnsAsync(game);
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        _kingdomsMock.Setup(k => k.AddAsync(It.IsAny<Kingdom>())).ReturnsAsync((Kingdom k) => k);

        // For JoinLobby, after commit it calls GetGameWithKingdomsAsync to build response
        var gameWithPlayers = MakeLobbyGame();
        gameWithPlayers.Kingdoms = new List<Kingdom>
        {
            MakeKingdom(UserId1, GameId),
            MakeKingdom(UserId2, GameId)
        };
        _gamesMock.Setup(g => g.GetGameWithKingdomsAsync(GameId)).ReturnsAsync(gameWithPlayers);
        _factionTypesMock.Setup(f => f.GetAllAsync()).ReturnsAsync(new List<FactionType>());

        var result = await _sut.JoinLobbyAsync(UserId2, new JoinLobbyRequest { InviteCode = "ABCDEF" });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    public async Task JoinLobby_InvalidCode_ReturnsFail()
    {
        _gamesMock.Setup(g => g.GetByLobbyCodeAsync(It.IsAny<string>())).ReturnsAsync((Game?)null);

        var result = await _sut.JoinLobbyAsync(UserId2, new JoinLobbyRequest { InviteCode = "XXXXXX" });

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task JoinLobby_LobbyFull_ReturnsFail()
    {
        var game = MakeLobbyGame(maxPlayers: 2);
        var kingdoms = new List<Kingdom>
        {
            MakeKingdom(UserId1, GameId),
            MakeKingdom(UserId2, GameId)
        };

        _gamesMock.Setup(g => g.GetByLobbyCodeAsync("ABCDEF")).ReturnsAsync(game);
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);

        var result = await _sut.JoinLobbyAsync(UserId3, new JoinLobbyRequest { InviteCode = "ABCDEF" });

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("full");
    }

    [Fact]
    public async Task JoinLobby_LobbyStarted_ReturnsFail()
    {
        var game = MakeLobbyGame(status: EGameStatus.InProgress);
        var kingdoms = new List<Kingdom> { MakeKingdom(UserId1, GameId) };

        _gamesMock.Setup(g => g.GetByLobbyCodeAsync("ABCDEF")).ReturnsAsync(game);
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);

        var result = await _sut.JoinLobbyAsync(UserId2, new JoinLobbyRequest { InviteCode = "ABCDEF" });

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task JoinLobby_AlreadyInLobby_ReturnsFail()
    {
        var game = MakeLobbyGame();
        var kingdoms = new List<Kingdom> { MakeKingdom(UserId2, GameId) };

        _gamesMock.Setup(g => g.GetByLobbyCodeAsync("ABCDEF")).ReturnsAsync(game);
        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);

        var result = await _sut.JoinLobbyAsync(UserId2, new JoinLobbyRequest { InviteCode = "ABCDEF" });

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("already");
    }

    // -------------------------------------------------------------------------
    // Leave lobby tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LeaveLobby_RegularPlayer_RemovesKingdom()
    {
        var game = MakeLobbyGame();
        var k1 = MakeKingdom(UserId1, GameId);
        var k2 = MakeKingdom(UserId2, GameId);
        var kingdoms = new List<Kingdom> { k1, k2 };

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        _kingdomsMock.Setup(k => k.DeleteAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        _gamesMock.Setup(g => g.UpdateAsync(It.IsAny<Game>())).ReturnsAsync((Game g) => g);

        // UserId2 (non-host) leaves
        var result = await _sut.LeaveLobbyAsync(UserId2, GameId);

        result.IsSuccess.ShouldBeTrue();
        _kingdomsMock.Verify(k => k.DeleteAsync(k2.Id), Times.Once);
        // Host should remain unchanged
        game.HostUserId.ShouldBe(UserId1);
    }

    [Fact]
    public async Task LeaveLobby_Host_TransfersToOldestJoiner()
    {
        var game = MakeLobbyGame();
        var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var hostKingdom = MakeKingdom(UserId1, GameId, createdAt: baseTime);
        // UserId2 joined before UserId3
        var player2 = MakeKingdom(UserId2, GameId, createdAt: baseTime.AddMinutes(5));
        var player3 = MakeKingdom(UserId3, GameId, createdAt: baseTime.AddMinutes(10));
        var kingdoms = new List<Kingdom> { hostKingdom, player2, player3 };

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        _kingdomsMock.Setup(k => k.DeleteAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        _gamesMock.Setup(g => g.UpdateAsync(It.IsAny<Game>())).ReturnsAsync((Game g) => g);

        var result = await _sut.LeaveLobbyAsync(UserId1, GameId);

        result.IsSuccess.ShouldBeTrue();
        // Host should transfer to UserId2 (earliest CreatedAt)
        game.HostUserId.ShouldBe(UserId2);
    }

    [Fact]
    public async Task LeaveLobby_Host_TransfersTiebreakById()
    {
        var game = MakeLobbyGame();
        var sameTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var hostKingdom = MakeKingdom(UserId1, GameId, createdAt: sameTime);

        // Two players with same CreatedAt — smaller Id should win
        var smallerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var largerId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var player2 = MakeKingdom(UserId2, GameId, createdAt: sameTime, id: largerId);
        var player3 = MakeKingdom(UserId3, GameId, createdAt: sameTime, id: smallerId);
        var kingdoms = new List<Kingdom> { hostKingdom, player2, player3 };

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        _kingdomsMock.Setup(k => k.DeleteAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        _gamesMock.Setup(g => g.UpdateAsync(It.IsAny<Game>())).ReturnsAsync((Game g) => g);

        var result = await _sut.LeaveLobbyAsync(UserId1, GameId);

        result.IsSuccess.ShouldBeTrue();
        // Player with smaller Id (UserId3/smallerId) should become host
        game.HostUserId.ShouldBe(UserId3);
    }

    [Fact]
    public async Task LeaveLobby_LastPlayer_ClosesLobby()
    {
        var game = MakeLobbyGame();
        var hostKingdom = MakeKingdom(UserId1, GameId);
        var kingdoms = new List<Kingdom> { hostKingdom };

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        _kingdomsMock.Setup(k => k.DeleteAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        _gamesMock.Setup(g => g.UpdateAsync(It.IsAny<Game>())).ReturnsAsync((Game g) => g);

        var result = await _sut.LeaveLobbyAsync(UserId1, GameId);

        result.IsSuccess.ShouldBeTrue();
        game.Status.ShouldBe(EGameStatus.Completed);
    }

    [Fact]
    public async Task LeaveLobby_NotInLobby_ReturnsFail()
    {
        var game = MakeLobbyGame();
        var kingdoms = new List<Kingdom> { MakeKingdom(UserId1, GameId) };

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);

        // UserId2 is not in this lobby
        var result = await _sut.LeaveLobbyAsync(UserId2, GameId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task LeaveLobby_GameStarted_ReturnsFail()
    {
        var game = MakeLobbyGame(status: EGameStatus.InProgress);

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);

        var result = await _sut.LeaveLobbyAsync(UserId1, GameId);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
    }

    // -------------------------------------------------------------------------
    // Select faction tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SelectFaction_Available_SetsOnKingdom()
    {
        var game = MakeLobbyGame();
        var playerKingdom = MakeKingdom(UserId1, GameId);
        var kingdoms = new List<Kingdom> { playerKingdom };
        var trackedKingdom = MakeKingdom(UserId1, GameId);

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _factionTypesMock.Setup(f => f.ExistsAsync(FactionId1)).ReturnsAsync(true);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        _kingdomsMock.Setup(k => k.GetKingdomByUserAndGameAsync(UserId1, GameId)).ReturnsAsync(trackedKingdom);
        _kingdomsMock.Setup(k => k.UpdateAsync(It.IsAny<Kingdom>())).ReturnsAsync((Kingdom k) => k);

        var result = await _sut.SelectFactionAsync(UserId1, GameId, FactionId1);

        result.IsSuccess.ShouldBeTrue();
        trackedKingdom.FactionTypeId.ShouldBe(FactionId1);
    }

    [Fact]
    public async Task SelectFaction_AlreadyTakenByOther_ReturnsFail()
    {
        var game = MakeLobbyGame();
        // UserId2 already has FactionId1
        var kingdoms = new List<Kingdom>
        {
            MakeKingdom(UserId1, GameId),
            MakeKingdom(UserId2, GameId, factionTypeId: FactionId1)
        };

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _factionTypesMock.Setup(f => f.ExistsAsync(FactionId1)).ReturnsAsync(true);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);

        var result = await _sut.SelectFactionAsync(UserId1, GameId, FactionId1);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("taken");
    }

    [Fact]
    public async Task SelectFaction_ChangeFaction_Succeeds()
    {
        var game = MakeLobbyGame();
        // UserId1 currently has FactionId1, switching to FactionId2
        var kingdoms = new List<Kingdom>
        {
            MakeKingdom(UserId1, GameId, factionTypeId: FactionId1)
        };
        var trackedKingdom = MakeKingdom(UserId1, GameId, factionTypeId: FactionId1);

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _factionTypesMock.Setup(f => f.ExistsAsync(FactionId2)).ReturnsAsync(true);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        _kingdomsMock.Setup(k => k.GetKingdomByUserAndGameAsync(UserId1, GameId)).ReturnsAsync(trackedKingdom);
        _kingdomsMock.Setup(k => k.UpdateAsync(It.IsAny<Kingdom>())).ReturnsAsync((Kingdom k) => k);

        var result = await _sut.SelectFactionAsync(UserId1, GameId, FactionId2);

        result.IsSuccess.ShouldBeTrue();
        trackedKingdom.FactionTypeId.ShouldBe(FactionId2);
    }

    [Fact]
    public async Task SelectFaction_NotInLobby_ReturnsFail()
    {
        var game = MakeLobbyGame();
        // Only UserId2 is in the lobby
        var kingdoms = new List<Kingdom> { MakeKingdom(UserId2, GameId) };

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _factionTypesMock.Setup(f => f.ExistsAsync(FactionId1)).ReturnsAsync(true);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);

        // UserId1 is not in the lobby
        var result = await _sut.SelectFactionAsync(UserId1, GameId, FactionId1);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task SelectFaction_InvalidFaction_ReturnsFail()
    {
        var game = MakeLobbyGame();
        var nonExistentFactionId = Guid.NewGuid();

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _factionTypesMock.Setup(f => f.ExistsAsync(nonExistentFactionId)).ReturnsAsync(false);

        var result = await _sut.SelectFactionAsync(UserId1, GameId, nonExistentFactionId);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("not found");
    }

    // -------------------------------------------------------------------------
    // Start game tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task StartGame_ValidConditions_TransitionsToInProgress()
    {
        var game = MakeLobbyGame();
        var kingdoms = new List<Kingdom>
        {
            MakeKingdom(UserId1, GameId, factionTypeId: FactionId1),
            MakeKingdom(UserId2, GameId, factionTypeId: FactionId2)
        };

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);
        _gamesMock.Setup(g => g.UpdateAsync(It.IsAny<Game>())).ReturnsAsync((Game g) => g);

        var result = await _sut.StartGameAsync(UserId1, GameId);

        result.IsSuccess.ShouldBeTrue();
        game.Status.ShouldBe(EGameStatus.InProgress);
    }

    [Fact]
    public async Task StartGame_NotHost_ReturnsFail()
    {
        var game = MakeLobbyGame(); // HostUserId = UserId1

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(new List<Kingdom>
        {
            MakeKingdom(UserId1, GameId, factionTypeId: FactionId1),
            MakeKingdom(UserId2, GameId, factionTypeId: FactionId2)
        });

        // UserId2 is not the host
        var result = await _sut.StartGameAsync(UserId2, GameId);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("host");
    }

    [Fact]
    public async Task StartGame_LessThan2Players_ReturnsFail()
    {
        var game = MakeLobbyGame();
        var kingdoms = new List<Kingdom>
        {
            MakeKingdom(UserId1, GameId, factionTypeId: FactionId1)
        };

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);

        var result = await _sut.StartGameAsync(UserId1, GameId);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("2");
    }

    [Fact]
    public async Task StartGame_MissingFactions_ReturnsFail()
    {
        var game = MakeLobbyGame();
        // UserId2 has no faction selected
        var kingdoms = new List<Kingdom>
        {
            MakeKingdom(UserId1, GameId, factionTypeId: FactionId1),
            MakeKingdom(UserId2, GameId, factionTypeId: null)
        };

        _gamesMock.Setup(g => g.GetByIdWithLockAsync(GameId)).ReturnsAsync(game);
        _kingdomsMock.Setup(k => k.GetKingdomsForGameAsync(GameId)).ReturnsAsync(kingdoms);

        var result = await _sut.StartGameAsync(UserId1, GameId);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.ShouldContain("faction");
    }

    // -------------------------------------------------------------------------
    // Get lobby tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetLobby_Exists_ReturnsFullResponse()
    {
        var faction1 = MakeFaction(FactionId1, "Warriors");
        var faction2 = MakeFaction(FactionId2, "Mages");

        var game = MakeLobbyGame();
        var k1 = MakeKingdom(UserId1, GameId, factionTypeId: FactionId1);
        k1.FactionType = faction1;
        var k2 = MakeKingdom(UserId2, GameId);
        game.Kingdoms = new List<Kingdom> { k1, k2 };

        _gamesMock.Setup(g => g.GetGameWithKingdomsAsync(GameId)).ReturnsAsync(game);
        _factionTypesMock.Setup(f => f.GetAllAsync()).ReturnsAsync(new List<FactionType> { faction1, faction2 });

        var result = await _sut.GetLobbyAsync(GameId);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Players.Count.ShouldBe(2);
        result.Value.Factions.Count.ShouldBe(2);
        // FactionId1 is taken by k1
        result.Value.Factions.First(f => f.FactionTypeId == FactionId1).IsAvailable.ShouldBeFalse();
        result.Value.Factions.First(f => f.FactionTypeId == FactionId2).IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public async Task GetLobby_NotFound_ReturnsFail()
    {
        _gamesMock.Setup(g => g.GetGameWithKingdomsAsync(It.IsAny<Guid>())).ReturnsAsync((Game?)null);

        var result = await _sut.GetLobbyAsync(Guid.NewGuid());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
    }

}
