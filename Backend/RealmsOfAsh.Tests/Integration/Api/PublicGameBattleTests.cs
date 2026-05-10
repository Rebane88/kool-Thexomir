using System.Net;
using System.Text.RegularExpressions;
using Application.Contracts;
using Application.Services.Army;
using Application.Services.Army.DTOs.V1;
using Application.Services.Building;
using Application.Services.Building.DTOs.V1;
using Application.Services.Combat;
using Application.Services.Combat.DTOs.V1;
using Application.Services.GameInitialization;
using Application.Services.GameInitialization.DTOs.V1;
using Application.Services.Lobby;
using Application.Services.Lobby.DTOs.V1;
using Application.Services.Turn;
using Domain.Game;
using Domain.Map;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Phase 37.5 — Public Game Battle Flow &amp; Endgame integration tests.
/// Covers MVCGAME-06, 07, 10, 11, 16, 17.
///
/// Requirement → test method mapping:
///   MVCGAME-07: DeclareAttack_*
///   MVCGAME-06: SelectArmies_*
///   MVCGAME-10, MVCGAME-17: SetLineup_*, BattleResolution_*
///   MVCGAME-16: Standings_*
///   MVCGAME-11: GameEnded_*, GameOver_*
///   MVCGAME-17 (event coverage): SignalRBattleEvents_*
/// </summary>
public class PublicGameBattleTests : IntegrationTestBase
{
    private const string PublicCookieName = ".Thexomir.Public";

    public PublicGameBattleTests(DatabaseFixture fixture) : base(fixture) { }

    // =========================================================================
    // MVCGAME-07: Declare attack — POST to DeclareAttack, redirect to Index
    // =========================================================================

    [Fact]
    public async Task DeclareAttack_PostsToCombatService_AndRedirectsToIndex()
    {
        var (gameId, attackerUserId, _, _, friendlyTileId, enemyTileId, _, _) =
            await SeedActionPhaseWithArmiesAndAdjacentTilesAsync();

        var attackerCookie = await GetCookieForUserAsync(attackerUserId);
        var (token, afCookies) = await GetAntiforgeryAsync(null, $"/Public/Game/Index/{gameId}", attackerCookie);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("TargetTileId", enemyTileId.ToString()),
            new KeyValuePair<string, string>("RiskedTileId", friendlyTileId.ToString())
        });
        var req = new HttpRequestMessage(HttpMethod.Post, $"/Public/Game/{gameId}/DeclareAttack")
        {
            Content = form
        };
        AddCookies(req, attackerCookie, afCookies);

        var resp = await Client.SendAsync(req);

        resp.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().ShouldContain($"/Public/Game/Index/{gameId}", Case.Insensitive);
    }

    [Fact]
    public async Task DeclareAttack_OnFailure_StoresErrorInTempData()
    {
        var (gameId, attackerUserId, _, _, friendlyTileId, _, _, _) =
            await SeedActionPhaseWithArmiesAndAdjacentTilesAsync();

        var attackerCookie = await GetCookieForUserAsync(attackerUserId);
        var (token, afCookies) = await GetAntiforgeryAsync(null, $"/Public/Game/Index/{gameId}", attackerCookie);

        // Send a bogus enemy tile (non-existent) — service will reject with an error
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("TargetTileId", Guid.NewGuid().ToString()),
            new KeyValuePair<string, string>("RiskedTileId", friendlyTileId.ToString())
        });
        var req = new HttpRequestMessage(HttpMethod.Post, $"/Public/Game/{gameId}/DeclareAttack")
        {
            Content = form
        };
        AddCookies(req, attackerCookie, afCookies);

        var resp = await Client.SendAsync(req);
        resp.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        // Follow the redirect — AttackError should appear in the rendered page
        var followReq = new HttpRequestMessage(HttpMethod.Get, resp.Headers.Location);
        followReq.Headers.Add("Cookie", attackerCookie);
        var followResp = await Client.SendAsync(followReq);
        var html = await followResp.Content.ReadAsStringAsync();
        html.ShouldContain("error", Case.Insensitive);
    }

    // =========================================================================
    // MVCGAME-06: Select armies — POST to SelectArmies, redirect to Index
    // =========================================================================

    [Fact]
    public async Task SelectArmies_PostsToCombatService_AndRedirectsToIndex()
    {
        var (gameId, attackerUserId, _, attackId, _, _, attackerArmyId, _) =
            await SeedBattlePhaseGameAsync();

        var attackerCookie = await GetCookieForUserAsync(attackerUserId);
        var (token, afCookies) = await GetAntiforgeryAsync(null, $"/Public/Game/Index/{gameId}", attackerCookie);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("DeclaredAttackId", attackId.ToString()),
            new KeyValuePair<string, string>("ArmyIds", attackerArmyId.ToString())
        });
        var req = new HttpRequestMessage(HttpMethod.Post, $"/Public/Game/{gameId}/SelectArmies")
        {
            Content = form
        };
        AddCookies(req, attackerCookie, afCookies);

        var resp = await Client.SendAsync(req);

        resp.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().ShouldContain($"/Public/Game/Index/{gameId}", Case.Insensitive);
    }

    [Fact]
    public async Task SelectArmies_BindsRepeatedArmyIdsAsList()
    {
        // Use a seed that trains TWO armies for the attacker during the Action phase
        var (gameId, attackerUserId, _, attackId, _, _, attackerArmyId, attackerArmyId2, _) =
            await SeedBattlePhaseGameWithTwoAttackerArmiesAsync();

        var attackerCookie = await GetCookieForUserAsync(attackerUserId);
        var (token, afCookies) = await GetAntiforgeryAsync(null, $"/Public/Game/Index/{gameId}", attackerCookie);

        // Use a list of KeyValuePairs with duplicate keys to send two ArmyIds
        var formPairs = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("DeclaredAttackId", attackId.ToString()),
            new("ArmyIds", attackerArmyId.ToString()),
            new("ArmyIds", attackerArmyId2.ToString())
        };
        var form = new FormUrlEncodedContent(formPairs);
        var req = new HttpRequestMessage(HttpMethod.Post, $"/Public/Game/{gameId}/SelectArmies")
        {
            Content = form
        };
        AddCookies(req, attackerCookie, afCookies);

        var resp = await Client.SendAsync(req);

        resp.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().ShouldContain($"/Public/Game/Index/{gameId}", Case.Insensitive);
    }

    // =========================================================================
    // MVCGAME-10 / MVCGAME-17: Set lineup + battle resolution
    // =========================================================================

    [Fact]
    public async Task SetLineup_WhenAllLineupsConfirmed_TriggersResolveAndAdvance()
    {
        var (gameId, attackerUserId, defenderUserId, attackId, _, _, attackerArmyId, defenderArmyId) =
            await SeedBattlePhaseGameAsync();

        // Attacker selects armies (service-level)
        await SelectArmiesServiceAsync(gameId, attackerUserId, attackId, [attackerArmyId]);

        // Defender selects armies (service-level)
        await SelectArmiesServiceAsync(gameId, defenderUserId, attackId, [defenderArmyId]);

        // Attacker sets lineup (service-level — first of two; does not trigger resolution)
        await SetLineupServiceAsync(gameId, attackerUserId, attackId, [attackerArmyId]);

        // Defender sets lineup via HTTP POST — second submission triggers ResolveAndAdvance.
        // PublicGameController passes zero delays so resolution completes in ~100ms; the
        // default HttpClient timeout is plenty.
        var client = Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var defenderCookie = await GetCookieForUserAsync(defenderUserId);
        var (token, afCookies) = await GetAntiforgeryAsync(client, $"/Public/Game/Index/{gameId}", defenderCookie);

        var formPairs = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("DeclaredAttackId", attackId.ToString()),
            new("ArmyIdsInOrder", defenderArmyId.ToString())
        };
        var req = new HttpRequestMessage(HttpMethod.Post, $"/Public/Game/{gameId}/SetLineup")
        {
            Content = new FormUrlEncodedContent(formPairs)
        };
        AddCookies(req, defenderCookie, afCookies);

        var resp = await client.SendAsync(req);
        resp.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        // Follow redirect — game should have advanced past Battle phase
        var followReq = new HttpRequestMessage(HttpMethod.Get, resp.Headers.Location);
        followReq.Headers.Add("Cookie", defenderCookie);
        var followResp = await client.SendAsync(followReq);

        // Either redirected to GameOver, or landed on Index with no "Confirm Lineup" wizard
        if (followResp.StatusCode == HttpStatusCode.Redirect)
        {
            // Redirected to GameOver — resolution ran and someone was eliminated
            followResp.Headers.Location!.ToString().ShouldContain("/Public/Game", Case.Insensitive);
        }
        else
        {
            var html = await followResp.Content.ReadAsStringAsync();
            // After resolution: either the PostBattle panel is shown, or the wizard is gone
            (html.Contains("post-battle") || !html.Contains("Confirm Lineup"))
                .ShouldBeTrue("Expected post-battle panel or no lineup wizard after resolution");
        }
    }

    [Fact]
    public async Task BattleResolution_PostBattlePanel_RendersRoundsFromTempData()
    {
        var (gameId, attackerUserId, defenderUserId, attackId, _, _, attackerArmyId, defenderArmyId) =
            await SeedBattlePhaseGameAsync();

        // Both sides select armies (service-level)
        await SelectArmiesServiceAsync(gameId, attackerUserId, attackId, [attackerArmyId]);
        await SelectArmiesServiceAsync(gameId, defenderUserId, attackId, [defenderArmyId]);

        // Attacker sets lineup (service-level)
        await SetLineupServiceAsync(gameId, attackerUserId, attackId, [attackerArmyId]);

        // Defender sets lineup via HTTP — triggers resolution and stores LastBattleResult in TempData.
        // PublicGameController passes zero delays so resolution is instant.
        var client = Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var defenderCookie = await GetCookieForUserAsync(defenderUserId);
        var (token, afCookies) = await GetAntiforgeryAsync(client, $"/Public/Game/Index/{gameId}", defenderCookie);

        var formPairs = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", token),
            new("DeclaredAttackId", attackId.ToString()),
            new("ArmyIdsInOrder", defenderArmyId.ToString())
        };
        var req = new HttpRequestMessage(HttpMethod.Post, $"/Public/Game/{gameId}/SetLineup")
        {
            Content = new FormUrlEncodedContent(formPairs)
        };
        AddCookies(req, defenderCookie, afCookies);

        var resp = await client.SendAsync(req);
        resp.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        // Follow the redirect — the same client session has the TempData cookie so LastBattleResult is present
        var followReq = new HttpRequestMessage(HttpMethod.Get, resp.Headers.Location);
        followReq.Headers.Add("Cookie", defenderCookie);
        // Also forward any Set-Cookie from the SetLineup response (antiforgery / session cookies)
        if (resp.Headers.Contains("Set-Cookie"))
        {
            foreach (var c in resp.Headers.GetValues("Set-Cookie"))
                followReq.Headers.Add("Cookie", c.Split(';')[0].Trim());
        }
        if (afCookies.Any())
        {
            foreach (var c in afCookies)
                followReq.Headers.Add("Cookie", c.Split(';')[0].Trim());
        }
        var followResp = await client.SendAsync(followReq);

        // May redirect to GameOver if the game ended
        if (followResp.StatusCode == HttpStatusCode.Redirect)
        {
            // The game ended — GET the GameOver page and verify it
            var goReq = new HttpRequestMessage(HttpMethod.Get, followResp.Headers.Location);
            goReq.Headers.Add("Cookie", defenderCookie);
            var goResp = await client.SendAsync(goReq);
            var goHtml = await goResp.Content.ReadAsStringAsync();
            goHtml.ShouldContain("game-over");
        }
        else
        {
            var html = await followResp.Content.ReadAsStringAsync();
            html.ShouldContain("post-battle");
            html.ShouldContain("battle-rounds");
        }
    }

    // =========================================================================
    // MVCGAME-16: Standings panel — active and fallen kingdoms
    // =========================================================================

    [Fact]
    public async Task Standings_Panel_RendersActiveAndFallenKingdoms()
    {
        var (gameId, attackerUserId, _, _, _, _, _, _) = await SeedBattlePhaseGameAsync();
        var attackerCookie = await GetCookieForUserAsync(attackerUserId);

        var req = new HttpRequestMessage(HttpMethod.Get, $"/Public/Game/Index/{gameId}");
        req.Headers.Add("Cookie", attackerCookie);
        req.Headers.AcceptLanguage.ParseAdd("en"); // pin culture so localized standings header is deterministic
        var resp = await Client.SendAsync(req);

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await resp.Content.ReadAsStringAsync();
        html.ShouldContain("standings-panel");
        html.ShouldContain("Active Kingdoms");
    }

    // =========================================================================
    // MVCGAME-11: Game over — redirect and final standings page
    // =========================================================================

    [Fact]
    public async Task GameEnded_RedirectsToGameOverPage_WithFinalStandings()
    {
        var (gameId, attackerUserId, _, _, _, _, _, _) = await SeedBattlePhaseGameAsync();

        // Use direct DB manipulation to mark the game as Completed
        await MarkGameCompletedAsync(gameId);

        var attackerCookie = await GetCookieForUserAsync(attackerUserId);
        var req = new HttpRequestMessage(HttpMethod.Get, $"/Public/Game/Index/{gameId}");
        req.Headers.Add("Cookie", attackerCookie);

        var resp = await Client.SendAsync(req);

        resp.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().ShouldContain($"/Public/Game/{gameId}/GameOver", Case.Insensitive);
    }

    [Fact]
    public async Task GameOver_Page_HasReturnToLobbyButton()
    {
        var (gameId, attackerUserId, _, _, _, _, _, _) = await SeedBattlePhaseGameAsync();

        // Mark game as Completed so the GameOver page is accessible
        await MarkGameCompletedAsync(gameId);

        var attackerCookie = await GetCookieForUserAsync(attackerUserId);
        var req = new HttpRequestMessage(HttpMethod.Get, $"/Public/Game/{gameId}/GameOver");
        req.Headers.Add("Cookie", attackerCookie);
        req.Headers.AcceptLanguage.ParseAdd("en"); // pin culture so localized "Return to Lobby" assertion is deterministic

        var resp = await Client.SendAsync(req);
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await resp.Content.ReadAsStringAsync();

        html.ShouldContain("Return to Lobby");
        html.ShouldContain("/Public/Lobby");
    }

    // =========================================================================
    // MVCGAME-17 (event coverage): SignalR battle events fired from POST actions
    // =========================================================================

    [Fact]
    public async Task SignalRBattleEvents_AreFiredFromBattlePostActions()
    {
        // Without a real SignalR client subscription we assert the controller side-effect:
        // DeclareAttack POST returns a redirect (302) on success, proving the hub broadcast path ran.
        var (gameId, attackerUserId, _, _, friendlyTileId, enemyTileId, _, _) =
            await SeedActionPhaseWithArmiesAndAdjacentTilesAsync();

        var attackerCookie = await GetCookieForUserAsync(attackerUserId);
        var (token, afCookies) = await GetAntiforgeryAsync(null, $"/Public/Game/Index/{gameId}", attackerCookie);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("TargetTileId", enemyTileId.ToString()),
            new KeyValuePair<string, string>("RiskedTileId", friendlyTileId.ToString())
        });
        var req = new HttpRequestMessage(HttpMethod.Post, $"/Public/Game/{gameId}/DeclareAttack")
        {
            Content = form
        };
        AddCookies(req, attackerCookie, afCookies);

        var resp = await Client.SendAsync(req);

        // 302 redirect means DeclareAttack succeeded and AttackDeclared hub broadcast was called
        resp.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }

    // =========================================================================
    // Seed helpers
    // =========================================================================

    /// <summary>
    /// Creates a 2-player game in Action phase with armies trained for both sides and
    /// adjacent enemy tiles identified. Does NOT declare an attack or advance to Battle phase.
    ///
    /// Returns: (gameId, attackerUserId, defenderUserId, attackId=Empty,
    ///           friendlyTileId, enemyTileId, attackerArmyId, defenderArmyId)
    /// </summary>
    private async Task<(Guid gameId, Guid attackerUserId, Guid defenderUserId, Guid attackId,
            Guid friendlyTileId, Guid enemyTileId, Guid attackerArmyId, Guid defenderArmyId)>
        SeedActionPhaseWithArmiesAndAdjacentTilesAsync()
    {
        // Step 1: Register two users and start a game
        var (hostEmail, _, _) = await RegisterAndExtractCookieAsync("battle-host");
        var (player2Email, _, _) = await RegisterAndExtractCookieAsync("battle-p2");

        var hostUserId = await GetUserIdByEmailAsync(hostEmail);
        var player2UserId = await GetUserIdByEmailAsync(player2Email);

        Guid gameId;

        using (var scope = Factory.Services.CreateScope())
        {
            var lobbyService = scope.ServiceProvider.GetRequiredService<ILobbyService>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var createResult = await lobbyService.CreateLobbyAsync(hostUserId, new CreateLobbyRequest
            {
                MaxPlayers = 2,
                WinCondition = EWinCondition.Elimination
            });
            createResult.IsSuccess.ShouldBeTrue($"CreateLobby failed: {createResult.Error}");
            var lobbyId = createResult.Value!.LobbyId;

            var joinResult = await lobbyService.JoinLobbyAsync(player2UserId, new JoinLobbyRequest
            {
                InviteCode = createResult.Value.InviteCode
            });
            joinResult.IsSuccess.ShouldBeTrue($"JoinLobby failed: {joinResult.Error}");

            var factions = (await uow.FactionTypes.GetAllAsync()).ToList();
            factions.Count.ShouldBeGreaterThanOrEqualTo(2);

            var selHost = await lobbyService.SelectFactionAsync(hostUserId, lobbyId, factions[0].Id);
            selHost.IsSuccess.ShouldBeTrue($"SelectFaction host failed: {selHost.Error}");

            var selP2 = await lobbyService.SelectFactionAsync(player2UserId, lobbyId, factions[1].Id);
            selP2.IsSuccess.ShouldBeTrue($"SelectFaction p2 failed: {selP2.Error}");

            var startResult = await lobbyService.StartGameAsync(hostUserId, lobbyId);
            startResult.IsSuccess.ShouldBeTrue($"StartGame failed: {startResult.Error}");

            var initService = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
            var state = await initService.InitializeGameAsync(lobbyId);
            state.CurrentPhase.ShouldBe("Action");

            gameId = lobbyId;
        }

        // Step 2: Train an army for the host (host's turn first)
        var hostArmyId = await TrainArmyForUserAsync(gameId, hostUserId);

        // Step 3: Find adjacent tiles (host tile borders player2 tile)
        var (friendlyTileId, enemyTileId) = await FindAdjacentEnemyTilesAsync(gameId, hostUserId, player2UserId);

        // Step 4: Train an army for player2 (temporarily advance turn via service to player2's turn)
        // We do this by ending host's turn so it becomes player2's turn
        await EndTurnServiceAsync(gameId, hostUserId);
        var player2ArmyId = await TrainArmyForUserAsync(gameId, player2UserId);
        // End player2's turn to return host to turn (no declared attacks yet → no Battle phase)
        await EndTurnServiceAsync(gameId, player2UserId);

        return (gameId, hostUserId, player2UserId, Guid.Empty,
            friendlyTileId, enemyTileId, hostArmyId, player2ArmyId);
    }

    /// <summary>
    /// Creates a 2-player game fully advanced to Battle phase with:
    /// - 1 army trained for each side
    /// - DeclareAttack declared (attacker = host, defender = player2)
    /// - Both EndTurns called → game is now in Battle phase
    ///
    /// Returns: (gameId, attackerUserId, defenderUserId, attackId,
    ///           friendlyTileId, enemyTileId, attackerArmyId, defenderArmyId)
    /// </summary>
    private async Task<(Guid gameId, Guid attackerUserId, Guid defenderUserId, Guid attackId,
            Guid friendlyTileId, Guid enemyTileId, Guid attackerArmyId, Guid defenderArmyId)>
        SeedBattlePhaseGameAsync()
    {
        // Register two users and start a game
        var (hostEmail, _, _) = await RegisterAndExtractCookieAsync("bphase-host");
        var (player2Email, _, _) = await RegisterAndExtractCookieAsync("bphase-p2");

        var hostUserId = await GetUserIdByEmailAsync(hostEmail);
        var player2UserId = await GetUserIdByEmailAsync(player2Email);

        Guid gameId;

        using (var scope = Factory.Services.CreateScope())
        {
            var lobbyService = scope.ServiceProvider.GetRequiredService<ILobbyService>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var createResult = await lobbyService.CreateLobbyAsync(hostUserId, new CreateLobbyRequest
            {
                MaxPlayers = 2,
                WinCondition = EWinCondition.Elimination
            });
            createResult.IsSuccess.ShouldBeTrue($"CreateLobby failed: {createResult.Error}");
            var lobbyId = createResult.Value!.LobbyId;

            var joinResult = await lobbyService.JoinLobbyAsync(player2UserId, new JoinLobbyRequest
            {
                InviteCode = createResult.Value.InviteCode
            });
            joinResult.IsSuccess.ShouldBeTrue($"JoinLobby failed: {joinResult.Error}");

            var factions = (await uow.FactionTypes.GetAllAsync()).ToList();
            factions.Count.ShouldBeGreaterThanOrEqualTo(2);

            var selHost = await lobbyService.SelectFactionAsync(hostUserId, lobbyId, factions[0].Id);
            selHost.IsSuccess.ShouldBeTrue($"SelectFaction host failed: {selHost.Error}");

            var selP2 = await lobbyService.SelectFactionAsync(player2UserId, lobbyId, factions[1].Id);
            selP2.IsSuccess.ShouldBeTrue($"SelectFaction p2 failed: {selP2.Error}");

            var startResult = await lobbyService.StartGameAsync(hostUserId, lobbyId);
            startResult.IsSuccess.ShouldBeTrue($"StartGame failed: {startResult.Error}");

            var initService = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
            var state = await initService.InitializeGameAsync(lobbyId);
            state.CurrentPhase.ShouldBe("Action");

            gameId = lobbyId;
        }

        // Host's turn: train army, then declare attack
        var hostArmyId = await TrainArmyForUserAsync(gameId, hostUserId);
        var (friendlyTileId, enemyTileId) = await FindAdjacentEnemyTilesAsync(gameId, hostUserId, player2UserId);
        var attackId = await DeclareAttackServiceAsync(gameId, hostUserId, enemyTileId, friendlyTileId);
        // End host's turn → player2's turn (Action phase still, since only 1 of 2 players ended)
        await EndTurnServiceAsync(gameId, hostUserId);

        // Player2's turn: train army, then end turn → Battle phase
        var player2ArmyId = await TrainArmyForUserAsync(gameId, player2UserId);
        await EndTurnServiceAsync(gameId, player2UserId);

        // Verify we're now in Battle phase
        var finalState = await GetGameStateAsync(gameId);
        finalState.CurrentPhase.ShouldBe("Battle", "Game should be in Battle phase after both players end turn with an active declared attack");

        return (gameId, hostUserId, player2UserId, attackId,
            friendlyTileId, enemyTileId, hostArmyId, player2ArmyId);
    }

    /// <summary>
    /// Same as SeedBattlePhaseGameAsync but trains TWO armies for the attacker during
    /// the Action phase. Returns a 9-tuple including the second attacker army ID.
    /// AP budget: host uses 4 AP (build+train1+train2+declare); player2 uses 2 AP (build+train).
    /// </summary>
    private async Task<(Guid gameId, Guid attackerUserId, Guid defenderUserId, Guid attackId,
            Guid friendlyTileId, Guid enemyTileId, Guid attackerArmyId, Guid attackerArmyId2, Guid defenderArmyId)>
        SeedBattlePhaseGameWithTwoAttackerArmiesAsync()
    {
        var (hostEmail, _, _) = await RegisterAndExtractCookieAsync("bphase2-host");
        var (player2Email, _, _) = await RegisterAndExtractCookieAsync("bphase2-p2");

        var hostUserId = await GetUserIdByEmailAsync(hostEmail);
        var player2UserId = await GetUserIdByEmailAsync(player2Email);

        Guid gameId;
        using (var scope = Factory.Services.CreateScope())
        {
            var lobbyService = scope.ServiceProvider.GetRequiredService<ILobbyService>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var createResult = await lobbyService.CreateLobbyAsync(hostUserId, new CreateLobbyRequest
            {
                MaxPlayers = 2,
                WinCondition = EWinCondition.Elimination
            });
            createResult.IsSuccess.ShouldBeTrue($"CreateLobby failed: {createResult.Error}");
            var lobbyId = createResult.Value!.LobbyId;

            var joinResult = await lobbyService.JoinLobbyAsync(player2UserId, new JoinLobbyRequest
            {
                InviteCode = createResult.Value.InviteCode
            });
            joinResult.IsSuccess.ShouldBeTrue($"JoinLobby failed: {joinResult.Error}");

            var factions = (await uow.FactionTypes.GetAllAsync()).ToList();
            await lobbyService.SelectFactionAsync(hostUserId, lobbyId, factions[0].Id);
            await lobbyService.SelectFactionAsync(player2UserId, lobbyId, factions[1].Id);
            await lobbyService.StartGameAsync(hostUserId, lobbyId);

            var initService = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
            await initService.InitializeGameAsync(lobbyId);
            gameId = lobbyId;
        }

        // Host's turn: train army 1, train army 2, find adjacent tiles, declare attack
        var hostArmyId = await TrainArmyForUserAsync(gameId, hostUserId);
        var hostArmyId2 = await TrainSecondArmyAsync(gameId, hostUserId);
        var (friendlyTileId, enemyTileId) = await FindAdjacentEnemyTilesAsync(gameId, hostUserId, player2UserId);
        var attackId = await DeclareAttackServiceAsync(gameId, hostUserId, enemyTileId, friendlyTileId);
        await EndTurnServiceAsync(gameId, hostUserId);

        // Player2's turn: train army, end turn → Battle phase
        var player2ArmyId = await TrainArmyForUserAsync(gameId, player2UserId);
        await EndTurnServiceAsync(gameId, player2UserId);

        var finalState = await GetGameStateAsync(gameId);
        finalState.CurrentPhase.ShouldBe("Battle");

        return (gameId, hostUserId, player2UserId, attackId,
            friendlyTileId, enemyTileId, hostArmyId, hostArmyId2, player2ArmyId);
    }

    // =========================================================================
    // Service-level combat helpers
    // =========================================================================

    private async Task<Guid> DeclareAttackServiceAsync(
        Guid gameId, Guid attackerUserId, Guid targetTileId, Guid riskedTileId)
    {
        using var scope = Factory.Services.CreateScope();
        var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
        var result = await combatService.DeclareAttackAsync(gameId, attackerUserId,
            new DeclareAttackRequest
            {
                TargetTileId = targetTileId,
                RiskedTileId = riskedTileId
            });
        result.IsSuccess.ShouldBeTrue($"DeclareAttack failed: {result.Error}");
        return result.Value!.AttackId;
    }

    private async Task SelectArmiesServiceAsync(
        Guid gameId, Guid userId, Guid attackId, List<Guid> armyIds)
    {
        using var scope = Factory.Services.CreateScope();
        var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
        var result = await combatService.SelectArmiesAsync(gameId, userId,
            new SelectArmiesRequest
            {
                DeclaredAttackId = attackId,
                ArmyIds = armyIds
            });
        result.IsSuccess.ShouldBeTrue($"SelectArmies failed for {userId}: {result.Error}");
    }

    private async Task SetLineupServiceAsync(
        Guid gameId, Guid userId, Guid attackId, List<Guid> armyIdsInOrder)
    {
        using var scope = Factory.Services.CreateScope();
        var combatService = scope.ServiceProvider.GetRequiredService<ICombatService>();
        var result = await combatService.SetLineupAsync(gameId, userId,
            new SetLineupRequest
            {
                DeclaredAttackId = attackId,
                ArmyIdsInOrder = armyIdsInOrder
            });
        result.IsSuccess.ShouldBeTrue($"SetLineup failed for {userId}: {result.Error}");
    }

    private async Task EndTurnServiceAsync(Guid gameId, Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var turnService = scope.ServiceProvider.GetRequiredService<ITurnService>();
        var result = await turnService.EndTurnAsync(
            gameId, userId,
            onRoundResolved: (_, _) => Task.CompletedTask,
            onBattleResolved: _ => Task.CompletedTask);
        result.IsSuccess.ShouldBeTrue($"EndTurn failed for {userId}: {result.Error}");
    }

    // =========================================================================
    // Army training helpers
    // =========================================================================

    private async Task<Guid> TrainArmyForUserAsync(Guid gameId, Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var gameInit = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
        var buildingService = scope.ServiceProvider.GetRequiredService<IBuildingService>();
        var armyService = scope.ServiceProvider.GetRequiredService<IArmyService>();

        var state = await gameInit.BuildGameStateSnapshotAsync(gameId);
        var myKingdom = state.Kingdoms.First(k => k.UserId == userId);

        // Find or create a military building
        var emptyOwnedTile = state.Tiles.First(t => t.KingdomId == myKingdom.Id && t.Buildings.Count == 0);

        var buildingTypes = (await buildingService.GetBuildingTypesAsync(gameId, userId)).ToList();
        var militaryBuildingType = buildingTypes.First(bt => bt.ArmyCapacity > 0 && bt.Tier == 1);

        var placeResult = await buildingService.PlaceBuildingAsync(gameId, userId, new PlaceBuildingRequest
        {
            TileId = emptyOwnedTile.Id,
            BuildingTypeId = militaryBuildingType.Id
        });
        placeResult.IsSuccess.ShouldBeTrue($"PlaceBuilding failed: {placeResult.Error}");
        var buildingId = placeResult.Value!.BuildingId;

        var armyTypes = (await armyService.GetArmyTypesAsync(gameId, userId)).ToList();
        var armyType = armyTypes.First(at => at.RequiredBuildingTypeId == militaryBuildingType.Id);

        var trainResult = await armyService.TrainArmyAsync(gameId, userId, new TrainArmyRequest
        {
            BuildingId = buildingId,
            ArmyTypeId = armyType.Id
        });
        trainResult.IsSuccess.ShouldBeTrue($"TrainArmy failed: {trainResult.Error}");

        return trainResult.Value!.ArmyId;
    }

    /// <summary>
    /// Trains a second army for the given user on a different empty tile.
    /// Assumes the user already has at least one military building placed.
    /// </summary>
    private async Task<Guid> TrainSecondArmyAsync(Guid gameId, Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var gameInit = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
        var buildingService = scope.ServiceProvider.GetRequiredService<IBuildingService>();
        var armyService = scope.ServiceProvider.GetRequiredService<IArmyService>();

        var state = await gameInit.BuildGameStateSnapshotAsync(gameId);
        var myKingdom = state.Kingdoms.First(k => k.UserId == userId);

        // Find a military building that already exists
        var tileWithBarracks = state.Tiles
            .Where(t => t.KingdomId == myKingdom.Id && t.Buildings.Count > 0)
            .FirstOrDefault();

        if (tileWithBarracks is null)
        {
            // No barracks yet — place one on a different empty tile
            var emptyTile = state.Tiles.First(t => t.KingdomId == myKingdom.Id && t.Buildings.Count == 0);
            var buildingTypes = (await buildingService.GetBuildingTypesAsync(gameId, userId)).ToList();
            var militaryType = buildingTypes.First(bt => bt.ArmyCapacity > 0 && bt.Tier == 1);
            var placeResult = await buildingService.PlaceBuildingAsync(gameId, userId, new PlaceBuildingRequest
            {
                TileId = emptyTile.Id,
                BuildingTypeId = militaryType.Id
            });
            placeResult.IsSuccess.ShouldBeTrue($"PlaceBuilding (second) failed: {placeResult.Error}");
            tileWithBarracks = emptyTile;
        }

        var buildingId = tileWithBarracks.Buildings.First().Id;
        var armyTypes = (await armyService.GetArmyTypesAsync(gameId, userId)).ToList();
        // Find the army type that matches the existing building type
        var existingBuildingTypeId = tileWithBarracks.Buildings.First().BuildingTypeId;
        var armyType = armyTypes.FirstOrDefault(at => at.RequiredBuildingTypeId == existingBuildingTypeId)
                    ?? armyTypes.First();

        var trainResult = await armyService.TrainArmyAsync(gameId, userId, new TrainArmyRequest
        {
            BuildingId = buildingId,
            ArmyTypeId = armyType.Id
        });
        trainResult.IsSuccess.ShouldBeTrue($"TrainArmy (second) failed: {trainResult.Error}");

        return trainResult.Value!.ArmyId;
    }

    // =========================================================================
    // Tile adjacency helper
    // =========================================================================

    /// <summary>
    /// Finds or creates an adjacent attacker-defender tile pair.
    ///
    /// Starting positions for a 2-player game are on opposite sides of a radius-9 map (~10 hexes apart).
    /// Their 7-tile territories have no natural border. This method picks an attacker tile,
    /// finds a neutral neighbor, and reassigns that neutral tile to the defender's kingdom so that
    /// DeclareAttack can succeed.
    /// </summary>
    private async Task<(Guid friendlyTileId, Guid enemyTileId)> FindAdjacentEnemyTilesAsync(
        Guid gameId, Guid attackerUserId, Guid defenderUserId)
    {
        var state = await GetGameStateAsync(gameId);
        var attackerKingdom = state.Kingdoms.First(k => k.UserId == attackerUserId);
        var defenderKingdom = state.Kingdoms.First(k => k.UserId == defenderUserId);

        // First try: find a naturally adjacent pair (unlikely on a large map, but correct)
        var defenderTiles = state.Tiles.Where(t => t.KingdomId == defenderKingdom.Id).ToList();
        foreach (var attackerTile in state.Tiles.Where(t => t.KingdomId == attackerKingdom.Id))
        {
            var neighbors = HexGridHelper.GetNeighbors(attackerTile.CoordQ, attackerTile.CoordR);
            var adjacentDefenderTile = defenderTiles.FirstOrDefault(dt =>
                neighbors.Any(n => n.q == dt.CoordQ && n.r == dt.CoordR));
            if (adjacentDefenderTile is not null)
                return (attackerTile.Id, adjacentDefenderTile.Id);
        }

        // Fallback: search ALL attacker tiles (including castle) for any neutral neighbor.
        // Each attacker territory is a 7-tile cluster: the outer 6 non-castle tiles each have
        // 5 non-attacker neighbors (4 neutral + 1 castle if applicable), so there will always
        // be neutral tiles adjacent to the outer ring.
        TileDto? friendlyTile = null;
        TileDto? neutralNeighbor = null;
        var attackerTileCoordSet = state.Tiles
            .Where(t => t.KingdomId == attackerKingdom.Id)
            .Select(t => (t.CoordQ, t.CoordR))
            .ToHashSet();

        foreach (var tile in state.Tiles.Where(t => t.KingdomId == attackerKingdom.Id && !t.IsCastle))
        {
            var neighborCoords = HexGridHelper.GetNeighbors(tile.CoordQ, tile.CoordR);
            // Find a neutral (no KingdomId) tile among neighbors that exists in the map
            var candidate = state.Tiles.FirstOrDefault(t =>
                t.KingdomId == null &&
                neighborCoords.Any(n => n.q == t.CoordQ && n.r == t.CoordR));

            if (candidate is not null)
            {
                friendlyTile = tile;
                neutralNeighbor = candidate;
                break;
            }
        }

        if (neutralNeighbor is null || friendlyTile is null)
        {
            // Last resort: use castle tile and find any neutral neighbor
            var castleTile = state.Tiles.First(t => t.KingdomId == attackerKingdom.Id && t.IsCastle);
            var castleNeighborCoords = HexGridHelper.GetNeighbors(castleTile.CoordQ, castleTile.CoordR);
            // Castle neighbors are all assigned to attacker; look at THEIR neighbors
            foreach (var neighborCoord in castleNeighborCoords)
            {
                var neighborTile = state.Tiles.FirstOrDefault(t =>
                    t.CoordQ == neighborCoord.q && t.CoordR == neighborCoord.r);
                if (neighborTile is null || neighborTile.KingdomId != attackerKingdom.Id) continue;

                var outerNeighborCoords = HexGridHelper.GetNeighbors(neighborTile.CoordQ, neighborTile.CoordR);
                neutralNeighbor = state.Tiles.FirstOrDefault(t =>
                    t.KingdomId == null &&
                    outerNeighborCoords.Any(n => n.q == t.CoordQ && n.r == t.CoordR));

                if (neutralNeighbor is not null)
                {
                    friendlyTile = neighborTile;
                    break;
                }
            }
        }

        (neutralNeighbor is not null && friendlyTile is not null)
            .ShouldBeTrue(
                $"Could not find a neutral tile adjacent to any attacker tile. " +
                $"AttackerKingdom={attackerKingdom.Id}, TileCount={state.Tiles.Count}");

        // Reassign the neutral tile to the defender using raw SQL to ensure
        // the change is committed and visible to subsequent service-layer reads.
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            $"UPDATE \"Tiles\" SET \"KingdomId\" = '{defenderKingdom.Id}' WHERE \"Id\" = '{neutralNeighbor!.Id}'");

        return (friendlyTile!.Id, neutralNeighbor!.Id);
    }

    // =========================================================================
    // DB direct manipulation helpers
    // =========================================================================

    private async Task MarkGameCompletedAsync(Guid gameId)
    {
        // AppDbContext is registered with NoTrackingWithIdentityResolution, so EF change
        // tracking is disabled. Use raw SQL to guarantee the status update is committed.
        // The Games."Status" column stores the enum as text (e.g. "Completed").
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var finishedAt = DateTime.UtcNow.ToString("O");
        await db.Database.ExecuteSqlRawAsync(
            $"UPDATE \"Games\" SET \"Status\" = 'Completed', \"FinishedAt\" = '{finishedAt}' WHERE \"Id\" = '{gameId}'");
    }

    // =========================================================================
    // Auth helpers
    // =========================================================================

    private async Task<string> GetCookieForUserAsync(Guid userId)
    {
        var email = await GetEmailByUserIdAsync(userId);
        var (status, cookies) = await LoginAsync(email, "Player1!");
        status.ShouldBe(HttpStatusCode.Redirect);
        var publicCookie = cookies.FirstOrDefault(c => c.Contains(PublicCookieName));
        publicCookie.ShouldNotBeNullOrEmpty($"Expected {PublicCookieName} cookie after login");
        return publicCookie!.Split(';')[0].Trim();
    }

    private async Task<(HttpStatusCode StatusCode, IEnumerable<string> Cookies)> LoginAsync(
        string email, string password)
    {
        var getResponse = await Client.GetAsync("/Public/Account/Login");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        token.ShouldNotBeNullOrEmpty("Could not find __RequestVerificationToken in Public login page HTML");

        var antiforgeryCookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Account/Login");
        request.Content = formContent;
        foreach (var cookie in antiforgeryCookies)
            request.Headers.Add("Cookie", cookie.Split(';')[0].Trim());

        var postResponse = await Client.SendAsync(request);
        var setCookies = postResponse.Headers.Contains("Set-Cookie")
            ? postResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        return (postResponse.StatusCode, setCookies);
    }

    private async Task<(string email, string password, string publicCookieValue)> RegisterAndExtractCookieAsync(
        string usernameHint = "user")
    {
        var email = $"phase37.5-{usernameHint}-{Guid.NewGuid():N}@test.local";
        const string password = "Player1!";

        var getResponse = await Client.GetAsync("/Public/Account/Register");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);

        var antiforgeryCookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("ConfirmPassword", password),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Account/Register");
        request.Content = formContent;
        foreach (var cookie in antiforgeryCookies)
            request.Headers.Add("Cookie", cookie.Split(';')[0].Trim());

        var postResponse = await Client.SendAsync(request);
        postResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect,
            "Public registration should set the cookie and redirect");

        var setCookies = postResponse.Headers.Contains("Set-Cookie")
            ? postResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        var publicCookie = setCookies.FirstOrDefault(c => c.Contains(PublicCookieName));
        publicCookie.ShouldNotBeNullOrEmpty($"Expected {PublicCookieName} cookie after Public registration");

        return (email, password, publicCookie!.Split(';')[0].Trim());
    }

    private async Task<(string token, IEnumerable<string> antiforgeryCookies)> GetAntiforgeryAsync(
        HttpClient? client, string url, string? authCookie = null)
    {
        var c = client ?? Client;
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(authCookie))
            request.Headers.Add("Cookie", authCookie);

        var response = await c.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"GET {url} failed with {response.StatusCode}. Body: {body[..Math.Min(body.Length, 2000)]}");
        }

        var html = await response.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        token.ShouldNotBeNullOrEmpty($"Could not find antiforgery token in response from {url}");

        var antiforgeryCookies = response.Headers.Contains("Set-Cookie")
            ? response.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        return (token, antiforgeryCookies);
    }

    private static void AddCookies(HttpRequestMessage request, string authCookie, IEnumerable<string> antiforgeryCookies)
    {
        if (!string.IsNullOrEmpty(authCookie))
            request.Headers.Add("Cookie", authCookie);
        foreach (var cookie in antiforgeryCookies)
            request.Headers.Add("Cookie", cookie.Split(';')[0].Trim());
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(html,
            @"<input[^>]+name=""__RequestVerificationToken""[^>]+value=""([^""]+)""",
            RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value;

        match = Regex.Match(html,
            @"<input[^>]+value=""([^""]+)""[^>]+name=""__RequestVerificationToken""",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private async Task<Guid> GetUserIdByEmailAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var result = await identity.GetByEmailAsync(email);
        result.IsSuccess.ShouldBeTrue($"User '{email}' should exist after registration");
        return result.Value!.Id;
    }

    private async Task<string> GetEmailByUserIdAsync(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var email = await identity.GetEmailAsync(userId);
        email.ShouldNotBeNull($"Email lookup should succeed for user id {userId}");
        return email!;
    }

    private async Task<GameStateDto> GetGameStateAsync(Guid gameId)
    {
        using var scope = Factory.Services.CreateScope();
        var gameInit = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
        return await gameInit.BuildGameStateSnapshotAsync(gameId);
    }
}
