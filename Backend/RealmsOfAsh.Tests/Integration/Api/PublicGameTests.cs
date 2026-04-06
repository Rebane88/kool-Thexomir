using System.Net;
using System.Text.RegularExpressions;
using Application.Contracts;
using Application.Services.GameInitialization;
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

    [Fact(Skip = "Wave 0 placeholder")]
    public async Task MVCGAME_01_HexMapRendersTerrainOwnershipCapitalsBuildingsArmies()
    {
        await Task.CompletedTask;
    }

    // =========================================================================
    // MVCGAME-02: Tile click drives contextual action panel
    // =========================================================================

    [Fact(Skip = "Wave 0 placeholder")]
    public async Task MVCGAME_02_TileClickDrivesContextualActionPanel()
    {
        await Task.CompletedTask;
    }

    // =========================================================================
    // MVCGAME-03: HUD shows resources, phase, turn, and action points
    // =========================================================================

    [Fact(Skip = "Wave 0 placeholder")]
    public async Task MVCGAME_03_HudShowsResourcesPhaseTurnAndActionPoints()
    {
        await Task.CompletedTask;
    }

    // =========================================================================
    // MVCGAME-04: Place building — valid POST places building via service
    // =========================================================================

    [Fact(Skip = "Wave 0 placeholder")]
    public async Task MVCGAME_04_PlaceBuilding_ValidPost_PlacesBuildingViaService()
    {
        await Task.CompletedTask;
    }

    // =========================================================================
    // MVCGAME-05: Train army — valid POST trains army via service
    // =========================================================================

    [Fact(Skip = "Wave 0 placeholder")]
    public async Task MVCGAME_05_TrainArmy_ValidPost_TrainsArmyViaService()
    {
        await Task.CompletedTask;
    }

    // =========================================================================
    // MVCGAME-08: Gamble spin — valid POST spins slot machine via service
    // =========================================================================

    [Fact(Skip = "Wave 0 placeholder")]
    public async Task MVCGAME_08_GambleSpin_ValidPost_SpinsSlotMachineViaService()
    {
        await Task.CompletedTask;
    }

    // =========================================================================
    // MVCGAME-09: End turn — valid POST ends turn via service
    // =========================================================================

    [Fact(Skip = "Wave 0 placeholder")]
    public async Task MVCGAME_09_EndTurn_ValidPost_EndsTurnViaService()
    {
        await Task.CompletedTask;
    }

    // =========================================================================
    // MVCGAME-12: Army roster — renders all owned armies with type, HP, location
    // =========================================================================

    [Fact(Skip = "Wave 0 placeholder")]
    public async Task MVCGAME_12_ArmyRoster_RendersAllOwnedArmiesWithTypeHpAndLocation()
    {
        await Task.CompletedTask;
    }

    // =========================================================================
    // MVCGAME-13: Building catalog — renders all types with costs, prereqs, affordability
    // =========================================================================

    [Fact(Skip = "Wave 0 placeholder")]
    public async Task MVCGAME_13_BuildingCatalog_RendersAllTypesWithCostsPrereqsAndAffordability()
    {
        await Task.CompletedTask;
    }

    // =========================================================================
    // MVCGAME-14: Abandon game — valid POST abandons and redirects to lobby
    // =========================================================================

    [Fact(Skip = "Wave 0 placeholder")]
    public async Task MVCGAME_14_AbandonGame_ValidPost_AbandonsAndRedirectsToLobby()
    {
        await Task.CompletedTask;
    }

    // =========================================================================
    // MVCRT-04: SignalR game event triggers client reload
    // =========================================================================

    [Fact(Skip = "Wave 0 placeholder")]
    public async Task MVCRT_04_SignalRGameEvent_TriggersClientReload()
    {
        await Task.CompletedTask;
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
}
