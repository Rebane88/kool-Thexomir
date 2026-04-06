using System.Net;
using System.Text.RegularExpressions;
using Application.Contracts;
using Application.Services.Lobby;
using Application.Services.Lobby.DTOs;
using Domain.Game;
using Microsoft.Extensions.DependencyInjection;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Phase 36 — Public Lobby UX & Real-Time integration tests.
/// Covers MVCLOBBY-01..07 and MVCRT-01.
/// All tests issue real HTTP requests through the WebApplicationFactory test client
/// and use cookie auth via the Public registration flow (matches PublicAuthTests pattern).
///
/// Isolation strategy: each test uses unique emails (Identity commits outside the EF
/// transaction scope, but lobby state is rolled back per-test by IntegrationTestBase).
/// </summary>
public class PublicLobbyTests : IntegrationTestBase
{
    private const string PublicCookieName = ".Thexomir.Public";

    public PublicLobbyTests(DatabaseFixture fixture) : base(fixture) { }

    // -------------------------------------------------------------------------
    // MVCLOBBY-01: Lobby index — authenticated sees list, anonymous redirects
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LobbyIndex_Authenticated_Returns200WithLobbyList()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/Public/Lobby");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("data-region=\"open-lobbies\"");
    }

    [Fact]
    public async Task LobbyIndex_Unauthenticated_Returns302ToLogin()
    {
        var response = await Client.GetAsync("/Public/Lobby");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString()
            .ShouldContain("/Public/Account/Login", Case.Insensitive);
    }

    // -------------------------------------------------------------------------
    // MVCLOBBY-02: Create lobby — valid data redirects, invalid shows error
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LobbyCreate_ValidData_Returns302ToDetail()
    {
        var (client, cookie) = await CreateAuthenticatedClientAsync();

        var (token, antiforgeryCookies) = await GetAntiforgeryAsync(client, "/Public/Lobby");

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("MaxPlayers", "4"),
            new KeyValuePair<string, string>("WinCondition", EWinCondition.Elimination.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Lobby/Create");
        request.Content = formContent;
        AddCookies(request, cookie, antiforgeryCookies);

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString()
            .ShouldMatch(@"/Public/Lobby/Detail/[0-9a-fA-F\-]{36}");
    }

    [Fact]
    public async Task LobbyCreate_InvalidMaxPlayers_Returns200WithError()
    {
        var (client, cookie) = await CreateAuthenticatedClientAsync();

        var (token, antiforgeryCookies) = await GetAntiforgeryAsync(client, "/Public/Lobby");

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("MaxPlayers", "99"),
            new KeyValuePair<string, string>("WinCondition", EWinCondition.Elimination.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Lobby/Create");
        request.Content = formContent;
        AddCookies(request, cookie, antiforgeryCookies);

        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        // Either the DataAnnotations Range error or the LobbyService error string is acceptable
        (html.Contains("MaxPlayers must be between") ||
         html.Contains("data-field-error=\"MaxPlayers\"") ||
         html.Contains("between 2 and"))
            .ShouldBeTrue("Expected MaxPlayers validation/service error in response HTML");
    }

    // -------------------------------------------------------------------------
    // MVCLOBBY-03: Join lobby — valid code joins, invalid/full show inline errors
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LobbyJoin_ValidInviteCode_Returns302ToDetail()
    {
        // User A creates a lobby via service so we get the invite code directly
        var (hostUserId, _) = await SeedAuthenticatedUserAsync();
        var (lobbyId, inviteCode) = await CreateLobbyViaServiceAsync(hostUserId, maxPlayers: 4);

        // User B authenticates via HTTP and joins via the Public/Lobby/Join form
        var (client, cookie) = await CreateAuthenticatedClientAsync();
        var (token, antiforgeryCookies) = await GetAntiforgeryAsync(client, "/Public/Lobby");

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("InviteCode", inviteCode),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Lobby/Join");
        request.Content = formContent;
        AddCookies(request, cookie, antiforgeryCookies);

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString()
            .ShouldContain($"/Public/Lobby/Detail/{lobbyId}", Case.Insensitive);
    }

    [Fact]
    public async Task LobbyJoin_InvalidCode_Returns200WithInlineError()
    {
        var (client, cookie) = await CreateAuthenticatedClientAsync();
        var (token, antiforgeryCookies) = await GetAntiforgeryAsync(client, "/Public/Lobby");

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("InviteCode", "ZZZZZZ"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Lobby/Join");
        request.Content = formContent;
        AddCookies(request, cookie, antiforgeryCookies);

        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        // The controller adds the LobbyService error to ModelState["InviteCode"]
        html.ShouldContain("Lobby not found.");
    }

    [Fact]
    public async Task LobbyJoin_FullLobby_Returns200WithInlineError()
    {
        // Host creates a 2-player lobby
        var (hostUserId, _) = await SeedAuthenticatedUserAsync();
        var (lobbyId, inviteCode) = await CreateLobbyViaServiceAsync(hostUserId, maxPlayers: 2);

        // Second player joins via service to fill the lobby
        var (player2UserId, _) = await SeedAuthenticatedUserAsync();
        await JoinLobbyViaServiceAsync(player2UserId, inviteCode);

        // User C tries to join via HTTP — should be rejected as full
        var (client, cookie) = await CreateAuthenticatedClientAsync();
        var (token, antiforgeryCookies) = await GetAntiforgeryAsync(client, "/Public/Lobby");

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("InviteCode", inviteCode),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Lobby/Join");
        request.Content = formContent;
        AddCookies(request, cookie, antiforgeryCookies);

        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Lobby is full.");
    }

    // -------------------------------------------------------------------------
    // MVCLOBBY-04: Lobby detail — member sees player list and invite code
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LobbyDetail_AuthenticatedMember_Returns200WithPlayerListAndInviteCode()
    {
        var (client, cookie, userId) = await CreateAuthenticatedClientWithUserIdAsync();
        var (lobbyId, inviteCode) = await CreateLobbyViaServiceAsync(userId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/Public/Lobby/Detail/{lobbyId}");
        AddCookies(request, cookie, Enumerable.Empty<string>());

        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain(inviteCode);
        html.ShouldContain("data-region=\"player-list\"");
        html.ShouldContain("data-marker=\"host\"");
    }

    // -------------------------------------------------------------------------
    // MVCLOBBY-05: Select faction — available succeeds, taken fails
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LobbySelectFaction_AvailableFaction_Returns302ToDetail()
    {
        var (client, cookie, userId) = await CreateAuthenticatedClientWithUserIdAsync();
        var (lobbyId, _) = await CreateLobbyViaServiceAsync(userId);

        var firstFactionId = await GetFirstFactionTypeIdAsync();

        var (token, antiforgeryCookies) = await GetAntiforgeryAsync(
            client, $"/Public/Lobby/Detail/{lobbyId}", cookie);

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("LobbyId", lobbyId.ToString()),
            new KeyValuePair<string, string>("FactionTypeId", firstFactionId.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Lobby/SelectFaction");
        request.Content = formContent;
        AddCookies(request, cookie, antiforgeryCookies);

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString()
            .ShouldContain($"/Public/Lobby/Detail/{lobbyId}", Case.Insensitive);
    }

    [Fact]
    public async Task LobbySelectFaction_TakenFaction_Returns200WithError()
    {
        // Host creates lobby, second player joins via service
        var (hostUserId, _) = await SeedAuthenticatedUserAsync();
        var (lobbyId, inviteCode) = await CreateLobbyViaServiceAsync(hostUserId);

        var (player2UserId, _) = await SeedAuthenticatedUserAsync();
        await JoinLobbyViaServiceAsync(player2UserId, inviteCode);

        // Pick a faction id and have host claim it via service
        var factionId = await GetFirstFactionTypeIdAsync();
        await SelectFactionViaServiceAsync(hostUserId, lobbyId, factionId);

        // Player 2 (authenticated via HTTP cookie) tries to pick the SAME faction
        var (client, cookie) = await CreateAuthenticatedClientForUserAsync(player2UserId);
        var (token, antiforgeryCookies) = await GetAntiforgeryAsync(
            client, $"/Public/Lobby/Detail/{lobbyId}", cookie);

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("LobbyId", lobbyId.ToString()),
            new KeyValuePair<string, string>("FactionTypeId", factionId.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Lobby/SelectFaction");
        request.Content = formContent;
        AddCookies(request, cookie, antiforgeryCookies);

        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("That faction is already taken by another player.");
    }

    // -------------------------------------------------------------------------
    // MVCLOBBY-06: Leave lobby — member exits back to lobby home
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LobbyLeave_Member_Returns302ToLobbyHome()
    {
        // Host creates lobby, player B joins via service
        var (hostUserId, _) = await SeedAuthenticatedUserAsync();
        var (lobbyId, inviteCode) = await CreateLobbyViaServiceAsync(hostUserId);

        var (player2UserId, _) = await SeedAuthenticatedUserAsync();
        await JoinLobbyViaServiceAsync(player2UserId, inviteCode);

        // Player B authenticates via HTTP cookie and POSTs Leave
        var (client, cookie) = await CreateAuthenticatedClientForUserAsync(player2UserId);
        var (token, antiforgeryCookies) = await GetAntiforgeryAsync(
            client, $"/Public/Lobby/Detail/{lobbyId}", cookie);

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/Public/Lobby/Leave/{lobbyId}");
        request.Content = formContent;
        AddCookies(request, cookie, antiforgeryCookies);

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString().ShouldContain("/Public/Lobby", Case.Insensitive);
    }

    // -------------------------------------------------------------------------
    // MVCLOBBY-07: Start game — host with 2+ players + factions succeeds; non-host fails
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LobbyStart_HostWith2PlayersAllFactions_Returns302()
    {
        // Host creates lobby, player 2 joins, both pick factions via service
        var (hostUserId, _) = await SeedAuthenticatedUserAsync();
        var (lobbyId, inviteCode) = await CreateLobbyViaServiceAsync(hostUserId);

        var (player2UserId, _) = await SeedAuthenticatedUserAsync();
        await JoinLobbyViaServiceAsync(player2UserId, inviteCode);

        var factionIds = await GetFirstTwoFactionTypeIdsAsync();
        await SelectFactionViaServiceAsync(hostUserId, lobbyId, factionIds.first);
        await SelectFactionViaServiceAsync(player2UserId, lobbyId, factionIds.second);

        // Host authenticates and POSTs Start
        var (client, cookie) = await CreateAuthenticatedClientForUserAsync(hostUserId);
        var (token, antiforgeryCookies) = await GetAntiforgeryAsync(
            client, $"/Public/Lobby/Detail/{lobbyId}", cookie);

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/Public/Lobby/Start/{lobbyId}");
        request.Content = formContent;
        AddCookies(request, cookie, antiforgeryCookies);

        var response = await client.SendAsync(request);

        // Phase 37 delivers Game/Index — assert only the 302 redirect, do not follow
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString()
            .ShouldContain($"/Public/Game/Index/{lobbyId}", Case.Insensitive);
    }

    [Fact]
    public async Task LobbyStart_NonHost_Returns200WithError()
    {
        // Same setup as the host-start test
        var (hostUserId, _) = await SeedAuthenticatedUserAsync();
        var (lobbyId, inviteCode) = await CreateLobbyViaServiceAsync(hostUserId);

        var (player2UserId, _) = await SeedAuthenticatedUserAsync();
        await JoinLobbyViaServiceAsync(player2UserId, inviteCode);

        var factionIds = await GetFirstTwoFactionTypeIdsAsync();
        await SelectFactionViaServiceAsync(hostUserId, lobbyId, factionIds.first);
        await SelectFactionViaServiceAsync(player2UserId, lobbyId, factionIds.second);

        // Non-host (player 2) tries to POST Start
        var (client, cookie) = await CreateAuthenticatedClientForUserAsync(player2UserId);
        var (token, antiforgeryCookies) = await GetAntiforgeryAsync(
            client, $"/Public/Lobby/Detail/{lobbyId}", cookie);

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/Public/Lobby/Start/{lobbyId}");
        request.Content = formContent;
        AddCookies(request, cookie, antiforgeryCookies);

        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Only the host can start the game.");
    }

    // -------------------------------------------------------------------------
    // MVCRT-01: Lobby detail page includes SignalR client script reference
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LobbyDetail_Html_ContainsSignalRScriptReference()
    {
        var (client, cookie, userId) = await CreateAuthenticatedClientWithUserIdAsync();
        var (lobbyId, _) = await CreateLobbyViaServiceAsync(userId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/Public/Lobby/Detail/{lobbyId}");
        AddCookies(request, cookie, Enumerable.Empty<string>());

        var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("signalr.min.js");
        html.ShouldContain("/hubs/game");
        html.ShouldContain("lobbyPlayerJoined");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Performs the public cookie login flow (copied verbatim from PublicAuthTests.cs):
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
    private async Task<(string email, string password, string publicCookieValue)> RegisterAndExtractCookieAsync()
    {
        var email = $"phase36-{Guid.NewGuid():N}@test.local";
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
        // Reuse the base Client (it already disables auto-redirect). Tests forward
        // the cookie explicitly per request via AddCookies.
        return (Client, publicCookieValue);
    }

    /// <summary>
    /// Same as CreateAuthenticatedClientAsync but also returns the resolved AppUser id
    /// for the registered user (looked up via IIdentityService).
    /// </summary>
    private async Task<(HttpClient client, string cookie, Guid userId)> CreateAuthenticatedClientWithUserIdAsync()
    {
        var (email, _, publicCookieValue) = await RegisterAndExtractCookieAsync();
        var userId = await GetUserIdByEmailAsync(email);
        return (Client, publicCookieValue, userId);
    }

    /// <summary>
    /// Seeds an authenticated user via the Public registration flow and returns
    /// just the user id (used when the test only needs server-side scope work).
    /// </summary>
    private async Task<(Guid userId, string email)> SeedAuthenticatedUserAsync()
    {
        var (email, _, _) = await RegisterAndExtractCookieAsync();
        var userId = await GetUserIdByEmailAsync(email);
        return (userId, email);
    }

    /// <summary>
    /// For a known seeded user id, register a fresh user via HTTP. NOTE: this is
    /// only used by tests that need a SECOND authenticated client for an already-
    /// seeded user. We instead seed a brand-new user via the registration flow
    /// and return the resulting cookie + the brand-new user id, then have the
    /// caller align lobby state to that id.
    /// </summary>
    private async Task<(HttpClient client, string cookie)> CreateAuthenticatedClientForUserAsync(Guid userId)
    {
        // Find the email for the given user id and login (the registration flow
        // already created the user, but the cookie was discarded — re-login).
        var email = await GetEmailByUserIdAsync(userId);
        var (status, cookies) = await LoginAsync(email, "Player1!");
        status.ShouldBe(HttpStatusCode.Redirect);
        var publicCookie = cookies.FirstOrDefault(c => c.Contains(PublicCookieName));
        publicCookie.ShouldNotBeNullOrEmpty($"Expected {PublicCookieName} cookie after login");
        return (Client, publicCookie!.Split(';')[0].Trim());
    }

    /// <summary>
    /// Creates a lobby via ILobbyService directly (bypassing HTTP) and returns
    /// the resulting lobby id and invite code.
    /// </summary>
    private async Task<(Guid lobbyId, string inviteCode)> CreateLobbyViaServiceAsync(
        Guid hostUserId, int maxPlayers = 4)
    {
        using var scope = Factory.Services.CreateScope();
        var lobbyService = scope.ServiceProvider.GetRequiredService<ILobbyService>();

        var result = await lobbyService.CreateLobbyAsync(hostUserId, new CreateLobbyRequest
        {
            MaxPlayers = maxPlayers,
            WinCondition = EWinCondition.Elimination
        });
        result.IsSuccess.ShouldBeTrue($"Failed to create lobby via service: {result.Error}");
        return (result.Value!.LobbyId, result.Value!.InviteCode);
    }

    private async Task JoinLobbyViaServiceAsync(Guid userId, string inviteCode)
    {
        using var scope = Factory.Services.CreateScope();
        var lobbyService = scope.ServiceProvider.GetRequiredService<ILobbyService>();

        var result = await lobbyService.JoinLobbyAsync(userId, new JoinLobbyRequest
        {
            InviteCode = inviteCode
        });
        result.IsSuccess.ShouldBeTrue($"Failed to join lobby via service: {result.Error}");
    }

    private async Task SelectFactionViaServiceAsync(Guid userId, Guid lobbyId, Guid factionTypeId)
    {
        using var scope = Factory.Services.CreateScope();
        var lobbyService = scope.ServiceProvider.GetRequiredService<ILobbyService>();

        var result = await lobbyService.SelectFactionAsync(userId, lobbyId, factionTypeId);
        result.IsSuccess.ShouldBeTrue($"Failed to select faction via service: {result.Error}");
    }

    /// <summary>
    /// Resolves the first available faction type id from the database via IUnitOfWork.
    /// </summary>
    private async Task<Guid> GetFirstFactionTypeIdAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var factions = (await uow.FactionTypes.GetAllAsync()).ToList();
        factions.ShouldNotBeEmpty("Seed data must include at least one FactionType");
        return factions[0].Id;
    }

    private async Task<(Guid first, Guid second)> GetFirstTwoFactionTypeIdsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var factions = (await uow.FactionTypes.GetAllAsync()).ToList();
        factions.Count.ShouldBeGreaterThanOrEqualTo(2, "Seed data must include at least two FactionTypes");
        return (factions[0].Id, factions[1].Id);
    }

    /// <summary>
    /// Resolves the AppUser id for an email via the IIdentityService registered in DI.
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
}
