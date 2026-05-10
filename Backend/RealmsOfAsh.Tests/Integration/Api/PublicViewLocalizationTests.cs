using System.Net;
using System.Text.RegularExpressions;
using Application.Contracts;
using Application.Services.GameInitialization;
using Application.Services.Lobby;
using Application.Services.Lobby.DTOs.V1;
using Domain.Game;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Phase 39 Public i18n Polish — integration tests for MVCI18N-02 (Razor view localization).
///
/// MVCI18N-02: Razor views under Areas/Public/ render Estonian strings when the
/// culture cookie is `et` and English strings when `en`. Covers lobby, game, and
/// auth views (the Register/ConfirmPassword gap from the research document).
///
/// Coverage map:
///   MVCI18N-02 (lobby index et)  → LobbyIndex_WithEtCulture_RendersEstonianStrings
///   MVCI18N-02 (lobby index en)  → LobbyIndex_WithEnCulture_RendersEnglishStrings
///   MVCI18N-02 (lobby detail et) → LobbyDetail_WithEtCulture_RendersEstonianLabels
///   MVCI18N-02 (game build et)   → GameViews_WithEtCulture_RendersEstonianBuildingPanel
///   MVCI18N-02 (game HUD et)     → GameViews_WithEtCulture_RendersEstonianHudLabels
///   MVCI18N-02 (register gap)    → RegisterPage_ConfirmPassword_LocalizedInBothCultures
/// </summary>
public class PublicViewLocalizationTests : IntegrationTestBase
{
    private const string PublicCookieName = ".Thexomir.Public";

    public PublicViewLocalizationTests(DatabaseFixture fixture) : base(fixture) { }

    // -------------------------------------------------------------------------
    // Lobby Index — both cultures
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LobbyIndex_WithEtCulture_RendersEstonianStrings()
    {
        var authCookie = await RegisterAndExtractCookieAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/Public/Lobby");
        AddCookies(request, authCookie, culture: "et");

        var response = await Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await ReadDecodedHtmlAsync(response);
        html.ShouldContain("Avatud mängulauad"); // Lobby_OpenLobbies (et)
        html.ShouldNotContain("Open Lobbies");    // English fallback must NOT leak
    }

    [Fact]
    public async Task LobbyIndex_WithEnCulture_RendersEnglishStrings()
    {
        var authCookie = await RegisterAndExtractCookieAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/Public/Lobby");
        AddCookies(request, authCookie, culture: "en");

        var response = await Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await ReadDecodedHtmlAsync(response);
        html.ShouldContain("Open Lobbies");        // Lobby_OpenLobbies (en)
        html.ShouldNotContain("Avatud mängulauad"); // Estonian must NOT leak
    }

    // -------------------------------------------------------------------------
    // Lobby Detail — Estonian labels
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LobbyDetail_WithEtCulture_RendersEstonianLabels()
    {
        var (authCookie, userId) = await RegisterAndExtractCookieWithUserIdAsync();
        var (lobbyId, _) = await CreateLobbyViaServiceAsync(userId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/Public/Lobby/Detail/{lobbyId}");
        AddCookies(request, authCookie, culture: "et");

        var response = await Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await ReadDecodedHtmlAsync(response);
        html.ShouldContain("Kutse kood");       // Lobby_InviteCode (et)
        html.ShouldContain("Vali fraktsioon");  // Lobby_PickFaction (et)
        html.ShouldNotContain("Invite Code");   // English must NOT leak
    }

    // -------------------------------------------------------------------------
    // Game views — Estonian building panel
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GameViews_WithEtCulture_RendersEstonianBuildingPanel()
    {
        var (gameId, hostCookie, _, _) = await SeedActiveGameAsync();

        // Select a tile owned by the host so the BuildingPanel renders (non-owned tiles
        // show the battle placeholder instead).
        var selectedTileId = await GetFirstOwnedTileIdAsync(gameId, hostCookie);
        var url = $"/Public/Game/Index/{gameId}?selectedTileId={selectedTileId}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddCookies(request, hostCookie, culture: "et");

        var response = await Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await ReadDecodedHtmlAsync(response);
        html.ShouldContain("Ehitised");      // Game_Buildings (et) — BuildingPanel header
        html.ShouldNotContain(">Buildings<"); // English heading text must NOT leak
    }

    // -------------------------------------------------------------------------
    // Game views — Estonian HUD labels
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GameViews_WithEtCulture_RendersEstonianHudLabels()
    {
        var (gameId, hostCookie, _, _) = await SeedActiveGameAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/Public/Game/Index/{gameId}");
        AddCookies(request, hostCookie, culture: "et");

        var response = await Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await ReadDecodedHtmlAsync(response);
        html.ShouldContain("Lõpeta käik");    // Game_Hud_EndTurn (et)
        html.ShouldContain("Voor");           // Game_Hud_Round (et)
        html.ShouldNotContain(">End Turn<");  // English button text must NOT leak
    }

    // -------------------------------------------------------------------------
    // Register page — ConfirmPassword localized in both cultures
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RegisterPage_ConfirmPassword_LocalizedInBothCultures()
    {
        // Et leg
        var etRequest = new HttpRequestMessage(HttpMethod.Get, "/Public/Account/Register");
        AddCookies(etRequest, authCookie: null, culture: "et");
        var etResponse = await Client.SendAsync(etRequest);
        etResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etHtml = await ReadDecodedHtmlAsync(etResponse);
        etHtml.ShouldContain("Kinnita parool"); // Form_ConfirmPassword (et)

        // En leg
        var enRequest = new HttpRequestMessage(HttpMethod.Get, "/Public/Account/Register");
        AddCookies(enRequest, authCookie: null, culture: "en");
        var enResponse = await Client.SendAsync(enRequest);
        enResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var enHtml = await ReadDecodedHtmlAsync(enResponse);
        enHtml.ShouldContain("Confirm password"); // Form_ConfirmPassword (en)
    }

    /// <summary>
    /// Reads the response body and HTML-decodes it so tests can match against raw
    /// Unicode strings (e.g. "Avatud mängulauad") instead of HTML-entity-escaped
    /// forms (e.g. "Avatud m&amp;#xE4;ngulauad"). Razor HTML-encodes non-ASCII
    /// characters emitted by the default writer.
    /// </summary>
    private static async Task<string> ReadDecodedHtmlAsync(HttpResponseMessage response)
    {
        var raw = await response.Content.ReadAsStringAsync();
        return WebUtility.HtmlDecode(raw);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Registers a fresh user via the Public registration flow and returns the
    /// resulting public auth cookie header value (e.g. ".Thexomir.Public=...").
    /// </summary>
    private async Task<string> RegisterAndExtractCookieAsync()
    {
        var (cookie, _) = await RegisterAndExtractCookieWithUserIdAsync();
        return cookie;
    }

    private async Task<(string authCookie, Guid userId)> RegisterAndExtractCookieWithUserIdAsync()
    {
        var email = $"phase39-{Guid.NewGuid():N}@test.local";
        const string password = "Player1!";

        var getResponse = await Client.GetAsync("/Public/Account/Register");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        token.ShouldNotBeNullOrEmpty("Could not find __RequestVerificationToken in Public register page HTML");

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

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Account/Register")
        {
            Content = formContent
        };
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
        publicCookie.ShouldNotBeNullOrEmpty($"Expected {PublicCookieName} cookie after Public registration");

        var authCookie = publicCookie!.Split(';')[0].Trim();
        var userId = await GetUserIdByEmailAsync(email);
        return (authCookie, userId);
    }

    private async Task<Guid> GetUserIdByEmailAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var result = await identity.GetByEmailAsync(email);
        result.IsSuccess.ShouldBeTrue($"User '{email}' should exist after registration");
        return result.Value!.Id;
    }

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

    /// <summary>
    /// Seeds two users, creates+joins a lobby, both pick factions, starts the game,
    /// and initializes the world. Returns (gameId, hostCookie, hostUserId, player2UserId).
    /// </summary>
    private async Task<(Guid gameId, string hostCookie, Guid hostUserId, Guid player2UserId)> SeedActiveGameAsync()
    {
        var (hostCookie, hostUserId) = await RegisterAndExtractCookieWithUserIdAsync();
        var (_, player2UserId) = await RegisterAndExtractCookieWithUserIdAsync();

        using var scope = Factory.Services.CreateScope();
        var lobbyService = scope.ServiceProvider.GetRequiredService<ILobbyService>();

        var createResult = await lobbyService.CreateLobbyAsync(hostUserId, new CreateLobbyRequest
        {
            MaxPlayers = 2,
            WinCondition = EWinCondition.Elimination
        });
        createResult.IsSuccess.ShouldBeTrue($"CreateLobby failed: {createResult.Error}");
        var lobbyId = createResult.Value!.LobbyId;
        var inviteCode = createResult.Value!.InviteCode;

        var joinResult = await lobbyService.JoinLobbyAsync(player2UserId, new JoinLobbyRequest
        {
            InviteCode = inviteCode
        });
        joinResult.IsSuccess.ShouldBeTrue($"JoinLobby failed: {joinResult.Error}");

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var factions = (await uow.FactionTypes.GetAllAsync()).ToList();
        factions.Count.ShouldBeGreaterThanOrEqualTo(2, "Seed data must include at least two FactionTypes");

        (await lobbyService.SelectFactionAsync(hostUserId, lobbyId, factions[0].Id))
            .IsSuccess.ShouldBeTrue();
        (await lobbyService.SelectFactionAsync(player2UserId, lobbyId, factions[1].Id))
            .IsSuccess.ShouldBeTrue();

        (await lobbyService.StartGameAsync(hostUserId, lobbyId)).IsSuccess.ShouldBeTrue();

        var initService = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
        var state = await initService.InitializeGameAsync(lobbyId);
        state.CurrentPhase.ShouldBe("Action");
        state.Tiles.Count.ShouldBeGreaterThan(0);

        return (lobbyId, hostCookie, hostUserId, player2UserId);
    }

    /// <summary>
    /// Returns the id of the first tile owned by the given host user (resolved via
    /// IGameInitializationService.BuildGameStateSnapshotAsync).
    /// </summary>
    private async Task<Guid> GetFirstOwnedTileIdAsync(Guid gameId, string hostCookie)
    {
        // hostCookie param kept for symmetry with HTTP tests; we resolve via service here.
        _ = hostCookie;
        using var scope = Factory.Services.CreateScope();
        var gameInit = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
        var snapshot = await gameInit.BuildGameStateSnapshotAsync(gameId);
        var ownedTile = snapshot.Tiles.First(t => t.KingdomId != null && t.Buildings.Count == 0);
        return ownedTile.Id;
    }

    /// <summary>
    /// Appends Cookie headers for an optional auth cookie AND an optional culture cookie.
    /// Each Cookie header value ends up as a separate entry; the server combines them.
    /// </summary>
    private static void AddCookies(HttpRequestMessage request, string? authCookie, string? culture)
    {
        if (!string.IsNullOrEmpty(authCookie))
        {
            request.Headers.Add("Cookie", authCookie);
        }
        if (!string.IsNullOrEmpty(culture))
        {
            var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
            request.Headers.Add(
                "Cookie",
                $"{CookieRequestCultureProvider.DefaultCookieName}={cookieValue}");
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
