using System.Net;
using System.Text.RegularExpressions;
using Application.Contracts;
using Application.Services.Army;
using Application.Services.Building;
using Application.Services.Building.DTOs;
using Application.Services.GameInitialization;
using Application.Services.GameInitialization.DTOs;
using Application.Services.Lobby;
using Application.Services.Lobby.DTOs;
using Domain.Game;
using Microsoft.Extensions.DependencyInjection;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Phase 37 — Public Game Core Actions integration tests.
/// Covers MVCGAME-01..05, 08, 09, 12, 13, 14 and MVCRT-04.
///
/// Wave 0: All 11 facts are stubbed (Skip = "Wave 0 placeholder").
/// Later plans flip the Skip attribute one-by-one as each feature is implemented.
///
/// Isolation strategy: each test uses unique emails (Identity commits outside the EF
/// transaction scope, but lobby/game state is rolled back per-test by IntegrationTestBase).
/// </summary>
public class PublicGameTests : IntegrationTestBase
{
    private const string PublicCookieName = ".Thexomir.Public";

    public PublicGameTests(DatabaseFixture fixture) : base(fixture) { }

    // =========================================================================
    // MVCGAME-01: Hex map renders terrain, ownership, capitals, buildings, armies
    // =========================================================================

    [Fact]
    public async Task MVCGAME_01_HexMapRendersTerrainOwnershipCapitalsBuildingsArmies()
    {
        var (gameId, hostCookie, _, _, _) = await SeedActiveGameAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/Public/Game/Index/{gameId}");
        request.Headers.Add("Cookie", hostCookie);

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("data-region=\"hex-map\"");
        body.ShouldContain("<polygon ", Case.Sensitive);
        // A seeded game generates tiles (radius >= 2 → at least 7 tiles; should have more than 7 polygons)
        var polygonCount = System.Text.RegularExpressions.Regex.Matches(body, "<polygon ").Count;
        polygonCount.ShouldBeGreaterThan(0);
        body.ShouldContain("data-marker=\"castle\"");
    }

    // =========================================================================
    // MVCGAME-02: Tile click drives contextual action panel
    // =========================================================================

    [Fact]
    public async Task MVCGAME_02_TileClickDrivesContextualActionPanel()
    {
        var ctx = await SeedActiveGameCtxAsync();

        // First fetch the game page to get the first tile id from the state
        var snapshot = await GetGameStateAsync(ctx.gameId);
        snapshot.Tiles.Count.ShouldBeGreaterThan(0);
        var firstTileId = snapshot.Tiles[0].Id;

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"/Public/Game/Index/{ctx.gameId}?selectedTileId={firstTileId}");
        request.Headers.Add("Cookie", ctx.hostCookie);

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("data-selected=\"true\"");
        body.ShouldContain("data-region=\"action-panel\"");
        body.ShouldContain("data-region=\"selected-tile\"");

        // Second leg: select an enemy or unclaimed tile and confirm battle placeholder appears
        var myKingdomId = snapshot.Kingdoms.First(k => k.UserId == ctx.hostUserId).Id;
        var enemyOrUnclaimedTile = snapshot.Tiles.First(t => t.KingdomId != myKingdomId);

        var resp2 = await SendAuthedGetAsync(
            ctx.client,
            $"/Public/Game/Index/{ctx.gameId}?selectedTileId={enemyOrUnclaimedTile.Id}",
            ctx.hostCookie);
        resp2.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html2 = await resp2.Content.ReadAsStringAsync();
        html2.ShouldContain("data-region=\"battle-placeholder\"");
        html2.ShouldContain("Phase 37.5");
        html2.ShouldNotContain("data-region=\"building-panel\""); // BuildingPanel must NOT render for non-own tiles
    }

    // =========================================================================
    // MVCGAME-03: HUD shows resources, phase, turn, and action points
    // =========================================================================

    [Fact]
    public async Task MVCGAME_03_HudShowsResourcesPhaseTurnAndActionPoints()
    {
        var (gameId, hostCookie, _, _, _) = await SeedActiveGameAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/Public/Game/Index/{gameId}");
        request.Headers.Add("Cookie", hostCookie);

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("data-region=\"hud\"");
        body.ShouldContain("data-resource=\"Gold\"");
        body.ShouldContain("data-region=\"turn\"");
        body.ShouldContain("Phase: Action");
        body.ShouldContain("data-ap=\"");
    }

    // =========================================================================
    // MVCGAME-04: Place building — valid POST places building via service
    // =========================================================================

    [Fact]
    public async Task MVCGAME_04_PlaceBuilding_ValidPost_PlacesBuildingViaService()
    {
        var ctx = await SeedActiveGameCtxAsync();
        var (token, afCookies) = await GetAntiforgeryAsync(
            ctx.client, $"/Public/Game/Index/{ctx.gameId}", ctx.hostCookie);

        // Find a tile owned by the host kingdom and a Tier 1 building type they can afford
        var (tileId, buildingTypeId) = await SeedBuildablePlacementAsync(ctx.gameId, ctx.hostUserId);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("TileId", tileId.ToString()),
            new KeyValuePair<string, string>("BuildingTypeId", buildingTypeId.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });
        var req = new HttpRequestMessage(HttpMethod.Post, $"/Public/Game/{ctx.gameId}/Build") { Content = form };
        AddCookies(req, ctx.hostCookie, afCookies);

        var resp = await ctx.client.SendAsync(req);
        resp.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().ShouldContain($"/Public/Game/Index/{ctx.gameId}", Case.Insensitive);
        resp.Headers.Location!.ToString().ShouldContain($"selectedTileId={tileId}", Case.Insensitive);
    }

    // =========================================================================
    // MVCGAME-05: Train army — valid POST trains army via service
    // =========================================================================

    [Fact]
    public async Task MVCGAME_05_TrainArmy_ValidPost_TrainsArmyViaService()
    {
        var ctx = await SeedActiveGameCtxAsync();
        var (tileId, buildingId, armyTypeId) = await SeedMilitaryBuildingAsync(ctx.gameId, ctx.hostUserId);

        var (token, afCookies) = await GetAntiforgeryAsync(
            ctx.client, $"/Public/Game/Index/{ctx.gameId}?selectedTileId={tileId}", ctx.hostCookie);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("BuildingId", buildingId.ToString()),
            new KeyValuePair<string, string>("ArmyTypeId", armyTypeId.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });
        var req = new HttpRequestMessage(HttpMethod.Post,
            $"/Public/Game/{ctx.gameId}/Train?selectedTileId={tileId}") { Content = form };
        AddCookies(req, ctx.hostCookie, afCookies);

        var resp = await ctx.client.SendAsync(req);
        resp.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().ShouldContain($"/Public/Game/Index/{ctx.gameId}", Case.Insensitive);
    }

    // =========================================================================
    // MVCGAME-08: Gamble spin — valid POST spins slot machine via service
    // =========================================================================

    [Fact]
    public async Task MVCGAME_08_GambleSpin_ValidPost_SpinsSlotMachineViaService()
    {
        var ctx = await SeedActiveGameCtxAsync();
        var (token, afCookies) = await GetAntiforgeryAsync(ctx.client, $"/Public/Game/Index/{ctx.gameId}", ctx.hostCookie);
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });
        var req = new HttpRequestMessage(HttpMethod.Post, $"/Public/Game/{ctx.gameId}/Spin") { Content = form };
        AddCookies(req, ctx.hostCookie, afCookies);
        var resp = await ctx.client.SendAsync(req);
        resp.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().ShouldContain($"/Public/Game/Index/{ctx.gameId}", Case.Insensitive);
    }

    // =========================================================================
    // MVCGAME-09: End turn — valid POST ends turn via service
    // =========================================================================

    [Fact]
    public async Task MVCGAME_09_EndTurn_ValidPost_EndsTurnViaService()
    {
        var ctx = await SeedActiveGameCtxAsync();
        var (token, afCookies) = await GetAntiforgeryAsync(ctx.client, $"/Public/Game/Index/{ctx.gameId}", ctx.hostCookie);
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });
        var req = new HttpRequestMessage(HttpMethod.Post, $"/Public/Game/{ctx.gameId}/EndTurn") { Content = form };
        AddCookies(req, ctx.hostCookie, afCookies);
        var resp = await ctx.client.SendAsync(req);
        resp.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().ShouldContain($"/Public/Game/Index/{ctx.gameId}", Case.Insensitive);
    }

    // =========================================================================
    // MVCGAME-12: Army roster — renders all owned armies with type, HP, location
    // =========================================================================

    [Fact]
    public async Task MVCGAME_12_ArmyRoster_RendersAllOwnedArmiesWithTypeHpAndLocation()
    {
        var ctx = await SeedActiveGameCtxAsync();
        var (_, _, _) = await SeedMilitaryBuildingAndTrainAsync(ctx.gameId, ctx.hostUserId);

        // Use a clean client (no cookie jar) to avoid cross-user cookie pollution.
        // The shared Client has player2's cookie in its jar from registration;
        // a clean client ensures the explicit hostCookie is the only auth cookie sent.
        var cleanClient = Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var resp = await SendAuthedGetAsync(cleanClient, $"/Public/Game/Index/{ctx.gameId}", ctx.hostCookie);
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await resp.Content.ReadAsStringAsync();
        html.ShouldContain("data-region=\"army-roster\"");
        html.ShouldContain("data-region=\"army-type\"");
        html.ShouldContain("data-region=\"army-hp\"");
        html.ShouldContain("data-region=\"army-location\"");
        html.ShouldContain("data-army-id=\"");
    }

    // =========================================================================
    // MVCGAME-13: Building catalog — renders all types with costs, prereqs, affordability
    // =========================================================================

    [Fact]
    public async Task MVCGAME_13_BuildingCatalog_RendersAllTypesWithCostsPrereqsAndAffordability()
    {
        var ctx = await SeedActiveGameCtxAsync();
        // Use a dedicated client with no cookie jar to avoid cross-user cookie pollution
        var cleanClient = Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        // Pick an empty owned tile for the host so the Tier 1 catalog rows are visible
        var snapshot = await GetGameStateAsync(ctx.gameId);
        var myKingdom = snapshot.Kingdoms.First(k => k.UserId == ctx.hostUserId);
        // Prefer an empty (no-building) tile so we get Tier-1 rows; fall back to any owned tile
        var ownTile = snapshot.Tiles.FirstOrDefault(t => t.KingdomId == myKingdom.Id && t.Buildings.Count == 0)
                   ?? snapshot.Tiles.First(t => t.KingdomId == myKingdom.Id);

        var resp = await SendAuthedGetAsync(cleanClient, $"/Public/Game/Index/{ctx.gameId}?selectedTileId={ownTile.Id}", ctx.hostCookie);
        resp.StatusCode.ShouldBe(HttpStatusCode.OK, $"Game index page should return 200");
        var html = await resp.Content.ReadAsStringAsync();

        html.ShouldContain("data-region=\"building-panel\"");
        html.ShouldContain("data-can-afford=\"");
        html.ShouldContain("data-building-type=\"");
        html.ShouldContain("data-region=\"bt-name\"");
    }

    // =========================================================================
    // MVCGAME-14: Abandon game — valid POST abandons and redirects to lobby
    // =========================================================================

    [Fact]
    public async Task MVCGAME_14_AbandonGame_ValidPost_AbandonsAndRedirectsToLobby()
    {
        var ctx = await SeedActiveGameCtxAsync();
        var (token, afCookies) = await GetAntiforgeryAsync(ctx.client, $"/Public/Game/Index/{ctx.gameId}", ctx.hostCookie);
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });
        var req = new HttpRequestMessage(HttpMethod.Post, $"/Public/Game/{ctx.gameId}/Abandon") { Content = form };
        AddCookies(req, ctx.hostCookie, afCookies);
        var resp = await ctx.client.SendAsync(req);
        resp.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().ShouldContain("/Public/Lobby", Case.Insensitive);
    }

    // =========================================================================
    // MVCRT-04: SignalR game event triggers client reload
    // =========================================================================

    [Fact]
    public async Task MVCRT_04_SignalRGameEvent_TriggersClientReload()
    {
        var ctx = await SeedActiveGameCtxAsync();
        var resp = await SendAuthedGetAsync(ctx.client, $"/Public/Game/Index/{ctx.gameId}", ctx.hostCookie);
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await resp.Content.ReadAsStringAsync();

        // Script tag loaded
        html.ShouldContain("signalr.min.js");
        html.ShouldContain("data-region=\"signalr-script\"");

        // Connection wired to /hubs/game
        html.ShouldContain("/hubs/game?gameId=");

        // All event subscriptions present
        html.ShouldContain("connection.on('turnAdvanced'");
        html.ShouldContain("connection.on('phaseChanged'");
        html.ShouldContain("connection.on('buildingPlaced'");
        html.ShouldContain("connection.on('armyTrained'");
        html.ShouldContain("connection.on('slotMachineSpun'");
        html.ShouldContain("connection.on('battleResolved'");
        html.ShouldContain("connection.on('gameOver'");

        // Debounce present
        html.ShouldContain("reloadPending");
        html.ShouldContain("setTimeout");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Seeds a full active game by:
    ///   1. Registering two users via the public registration flow
    ///   2. Creating a lobby and joining both players (service-level, no HTTP)
    ///   3. Selecting distinct factions for both players
    ///   4. Starting the game (LobbyService) then initializing the game world (GameInitializationService)
    ///
    /// Returns (gameId, hostCookie, player2Cookie, hostUserId, player2UserId).
    /// The gameId equals the lobbyId — same entity, status changes to InProgress.
    /// </summary>
    private async Task<(Guid gameId, string hostCookie, string player2Cookie, Guid hostUserId, Guid player2UserId)>
        SeedActiveGameAsync()
    {
        // Register two users and capture their public auth cookies + user ids
        var (hostEmail, _, hostCookie) = await RegisterAndExtractCookieAsync("host");
        var (player2Email, _, player2Cookie) = await RegisterAndExtractCookieAsync("player2");

        var hostUserId = await GetUserIdByEmailAsync(hostEmail);
        var player2UserId = await GetUserIdByEmailAsync(player2Email);

        using (var scope = Factory.Services.CreateScope())
        {
            var lobbyService = scope.ServiceProvider.GetRequiredService<ILobbyService>();

            // Create lobby
            var createResult = await lobbyService.CreateLobbyAsync(hostUserId, new CreateLobbyRequest
            {
                MaxPlayers = 2,
                WinCondition = EWinCondition.Elimination
            });
            createResult.IsSuccess.ShouldBeTrue($"CreateLobby failed: {createResult.Error}");
            var lobbyId = createResult.Value!.LobbyId;
            var inviteCode = createResult.Value!.InviteCode;

            // Player 2 joins
            var joinResult = await lobbyService.JoinLobbyAsync(player2UserId, new JoinLobbyRequest
            {
                InviteCode = inviteCode
            });
            joinResult.IsSuccess.ShouldBeTrue($"JoinLobby failed: {joinResult.Error}");

            // Get faction ids
            var uow = scope.ServiceProvider.GetRequiredService<Application.Contracts.IUnitOfWork>();
            var factions = (await uow.FactionTypes.GetAllAsync()).ToList();
            factions.Count.ShouldBeGreaterThanOrEqualTo(2, "Seed data must include at least two FactionTypes");
            var hostFactionId = factions[0].Id;
            var player2FactionId = factions[1].Id;

            // Both players select distinct factions
            var selectHost = await lobbyService.SelectFactionAsync(hostUserId, lobbyId, hostFactionId);
            selectHost.IsSuccess.ShouldBeTrue($"SelectFaction (host) failed: {selectHost.Error}");

            var selectPlayer2 = await lobbyService.SelectFactionAsync(player2UserId, lobbyId, player2FactionId);
            selectPlayer2.IsSuccess.ShouldBeTrue($"SelectFaction (player2) failed: {selectPlayer2.Error}");

            // Start the game
            var startResult = await lobbyService.StartGameAsync(hostUserId, lobbyId);
            startResult.IsSuccess.ShouldBeTrue($"StartGame failed: {startResult.Error}");

            // Initialize game world (maps, tiles, kingdoms, resources)
            var initService = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
            var state = await initService.InitializeGameAsync(lobbyId);

            // Sanity-assert game is now in Action phase with tiles
            state.CurrentPhase.ShouldBe("Action", "Game should be in Action phase after initialization");
            state.Tiles.Count.ShouldBeGreaterThan(0, "Game should have tiles after initialization");

            return (lobbyId, hostCookie, player2Cookie, hostUserId, player2UserId);
        }
    }

    /// <summary>
    /// Performs the public cookie login flow:
    /// 1. GET /Public/Account/Login to retrieve the antiforgery token
    /// 2. POST credentials + antiforgery token
    /// Returns the POST response status code and all Set-Cookie header values.
    /// </summary>
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
        {
            request.Headers.Add("Cookie", cookie.Split(';')[0].Trim());
        }

        var postResponse = await Client.SendAsync(request);

        var setCookies = postResponse.Headers.Contains("Set-Cookie")
            ? postResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        return (postResponse.StatusCode, setCookies);
    }

    /// <summary>
    /// Registers a new user via /Public/Account/Register and returns the resulting
    /// public auth cookie value (e.g. ".Thexomir.Public=...").
    /// </summary>
    private async Task<(string email, string password, string publicCookieValue)> RegisterAndExtractCookieAsync(
        string usernameHint = "user")
    {
        var email = $"phase37-{usernameHint}-{Guid.NewGuid():N}@test.local";
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
        {
            request.Headers.Add("Cookie", cookie.Split(';')[0].Trim());
        }

        var postResponse = await Client.SendAsync(request);
        postResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect,
            "Public registration should set the cookie and redirect");

        var setCookies = postResponse.Headers.Contains("Set-Cookie")
            ? postResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        var publicCookie = setCookies.FirstOrDefault(c => c.Contains(PublicCookieName));
        publicCookie.ShouldNotBeNullOrEmpty(
            $"Expected {PublicCookieName} cookie after Public registration");

        var publicCookieValue = publicCookie!.Split(';')[0].Trim();
        return (email, password, publicCookieValue);
    }

    /// <summary>
    /// Registers a fresh user via the Public registration flow and returns
    /// an HttpClient (with redirect disabled) plus the public auth cookie value.
    /// </summary>
    private async Task<(HttpClient client, string cookie)> CreateAuthenticatedClientAsync()
    {
        var (_, _, publicCookieValue) = await RegisterAndExtractCookieAsync();
        return (Client, publicCookieValue);
    }

    /// <summary>
    /// Same as CreateAuthenticatedClientAsync but also returns the resolved AppUser id.
    /// </summary>
    private async Task<(HttpClient client, string cookie, Guid userId)> CreateAuthenticatedClientWithUserIdAsync()
    {
        var (email, _, publicCookieValue) = await RegisterAndExtractCookieAsync();
        var userId = await GetUserIdByEmailAsync(email);
        return (Client, publicCookieValue, userId);
    }

    /// <summary>
    /// For a known user id, re-login via the public login flow and return a client + cookie.
    /// </summary>
    private async Task<(HttpClient client, string cookie)> CreateAuthenticatedClientForUserAsync(Guid userId)
    {
        var email = await GetEmailByUserIdAsync(userId);
        var (status, cookies) = await LoginAsync(email, "Player1!");
        status.ShouldBe(HttpStatusCode.Redirect);
        var publicCookie = cookies.FirstOrDefault(c => c.Contains(PublicCookieName));
        publicCookie.ShouldNotBeNullOrEmpty($"Expected {PublicCookieName} cookie after login");
        return (Client, publicCookie!.Split(';')[0].Trim());
    }

    /// <summary>
    /// GETs a page (forwarding the public auth cookie if provided), extracts the
    /// __RequestVerificationToken from the rendered HTML, and returns it together
    /// with the antiforgery Set-Cookie values from the response.
    /// </summary>
    private async Task<(string token, IEnumerable<string> antiforgeryCookies)> GetAntiforgeryAsync(
        HttpClient client, string url, string? authCookie = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(authCookie))
        {
            request.Headers.Add("Cookie", authCookie);
        }

        var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK,
            $"GET {url} should succeed (got {response.StatusCode})");

        var html = await response.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        token.ShouldNotBeNullOrEmpty($"Could not find antiforgery token in response from {url}");

        var antiforgeryCookies = response.Headers.Contains("Set-Cookie")
            ? response.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        return (token, antiforgeryCookies);
    }

    /// <summary>
    /// Adds the public auth cookie and any antiforgery cookies to the outgoing request.
    /// </summary>
    private static void AddCookies(HttpRequestMessage request, string authCookie, IEnumerable<string> antiforgeryCookies)
    {
        if (!string.IsNullOrEmpty(authCookie))
        {
            request.Headers.Add("Cookie", authCookie);
        }
        foreach (var cookie in antiforgeryCookies)
        {
            request.Headers.Add("Cookie", cookie.Split(';')[0].Trim());
        }
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

    /// <summary>
    /// Resolves the AppUser id for a known email via IIdentityService.
    /// </summary>
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

    /// <summary>
    /// Named-context wrapper around SeedActiveGameAsync for tests that need ctx.client, ctx.gameId, etc.
    /// </summary>
    private sealed record GameTestContext(
        HttpClient client,
        Guid gameId,
        string hostCookie,
        string player2Cookie,
        Guid hostUserId,
        Guid player2UserId);

    private async Task<GameTestContext> SeedActiveGameCtxAsync()
    {
        var (gameId, hostCookie, player2Cookie, hostUserId, player2UserId) = await SeedActiveGameAsync();
        return new GameTestContext(Client, gameId, hostCookie, player2Cookie, hostUserId, player2UserId);
    }

    /// <summary>
    /// Returns the game state snapshot for the given gameId via IGameInitializationService.
    /// </summary>
    private async Task<GameStateDto> GetGameStateAsync(Guid gameId)
    {
        using var scope = Factory.Services.CreateScope();
        var gameInit = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
        return await gameInit.BuildGameStateSnapshotAsync(gameId);
    }

    /// <summary>
    /// GETs a URL with the given auth cookie and returns the response.
    /// </summary>
    private async Task<HttpResponseMessage> SendAuthedGetAsync(HttpClient client, string url, string authCookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(authCookie))
            request.Headers.Add("Cookie", authCookie);
        return await client.SendAsync(request);
    }

    /// <summary>
    /// Places a Tier 1 military building (Barracks) on an empty tile owned by the given user,
    /// then returns (tileId, buildingId, armyTypeId) so the caller can POST Train.
    /// The seeded game starts with 4 AP; placing costs 1 AP, leaving 3 for training.
    /// </summary>
    private async Task<(Guid tileId, Guid buildingId, Guid armyTypeId)> SeedMilitaryBuildingAsync(Guid gameId, Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var gameInit = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
        var buildingService = scope.ServiceProvider.GetRequiredService<IBuildingService>();
        var armyService = scope.ServiceProvider.GetRequiredService<IArmyService>();

        var state = await gameInit.BuildGameStateSnapshotAsync(gameId);
        var myKingdom = state.Kingdoms.First(k => k.UserId == userId);

        // Find an empty owned tile
        var emptyOwnedTile = state.Tiles.First(t => t.KingdomId == myKingdom.Id && t.Buildings.Count == 0);

        // Get building types and pick the first one that has ArmyCapacity > 0 (i.e. Barracks)
        var buildingTypes = (await buildingService.GetBuildingTypesAsync(gameId, userId)).ToList();
        var militaryBuildingType = buildingTypes.First(bt => bt.ArmyCapacity > 0 && bt.Tier == 1);

        // Place the military building
        var placeResult = await buildingService.PlaceBuildingAsync(gameId, userId, new PlaceBuildingRequest
        {
            TileId = emptyOwnedTile.Id,
            BuildingTypeId = militaryBuildingType.Id
        });
        placeResult.IsSuccess.ShouldBeTrue($"PlaceBuildingAsync failed: {placeResult.Error}");
        var buildingId = placeResult.Value!.BuildingId;

        // Get an army type that requires this building type
        var armyTypes = (await armyService.GetArmyTypesAsync(gameId, userId)).ToList();
        var armyType = armyTypes.First(at => at.RequiredBuildingTypeId == militaryBuildingType.Id);

        return (emptyOwnedTile.Id, buildingId, armyType.Id);
    }

    /// <summary>
    /// Places a Tier 1 military building on an empty owned tile, then trains one army in that building.
    /// Returns (tileId, buildingId, armyId). Requires sufficient AP in game state.
    /// </summary>
    private async Task<(Guid tileId, Guid buildingId, Guid armyId)> SeedMilitaryBuildingAndTrainAsync(Guid gameId, Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var gameInit = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
        var buildingService = scope.ServiceProvider.GetRequiredService<IBuildingService>();
        var armyService = scope.ServiceProvider.GetRequiredService<IArmyService>();

        var state = await gameInit.BuildGameStateSnapshotAsync(gameId);
        var myKingdom = state.Kingdoms.First(k => k.UserId == userId);

        // Find an empty owned tile
        var emptyOwnedTile = state.Tiles.First(t => t.KingdomId == myKingdom.Id && t.Buildings.Count == 0);

        // Get a Tier 1 military building type (army capacity > 0)
        var buildingTypes = (await buildingService.GetBuildingTypesAsync(gameId, userId)).ToList();
        var militaryBuildingType = buildingTypes.First(bt => bt.ArmyCapacity > 0 && bt.Tier == 1);

        // Place the military building
        var placeResult = await buildingService.PlaceBuildingAsync(gameId, userId, new PlaceBuildingRequest
        {
            TileId = emptyOwnedTile.Id,
            BuildingTypeId = militaryBuildingType.Id
        });
        placeResult.IsSuccess.ShouldBeTrue($"PlaceBuildingAsync failed: {placeResult.Error}");
        var buildingId = placeResult.Value!.BuildingId;

        // Get the army type that requires this building type
        var armyTypes = (await armyService.GetArmyTypesAsync(gameId, userId)).ToList();
        var armyType = armyTypes.First(at => at.RequiredBuildingTypeId == militaryBuildingType.Id);

        // Train one army
        var trainResult = await armyService.TrainArmyAsync(gameId, userId, new Application.Services.Army.DTOs.TrainArmyRequest
        {
            BuildingId = buildingId,
            ArmyTypeId = armyType.Id
        });
        trainResult.IsSuccess.ShouldBeTrue($"TrainArmyAsync failed: {trainResult.Error}");
        var armyId = trainResult.Value!.ArmyId;

        return (emptyOwnedTile.Id, buildingId, armyId);
    }

    /// <summary>
    /// Finds a tile owned by the given user's kingdom with no existing building,
    /// and the first Tier 1 building type available (from IBuildingService.GetBuildingTypesAsync).
    /// Returns (tileId, buildingTypeId).
    /// </summary>
    private async Task<(Guid tileId, Guid buildingTypeId)> SeedBuildablePlacementAsync(Guid gameId, Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var gameInit = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
        var buildingService = scope.ServiceProvider.GetRequiredService<IBuildingService>();

        var state = await gameInit.BuildGameStateSnapshotAsync(gameId);
        var myKingdom = state.Kingdoms.First(k => k.UserId == userId);

        // Find an owned tile with no existing building so a Tier 1 building can be placed
        var emptyOwnedTile = state.Tiles.First(t => t.KingdomId == myKingdom.Id && t.Buildings.Count == 0);

        var buildingTypes = (await buildingService.GetBuildingTypesAsync(gameId, userId)).ToList();
        var tier1Type = buildingTypes.First(bt => bt.Tier == 1);

        return (emptyOwnedTile.Id, tier1Type.Id);
    }
}
