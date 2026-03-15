using System.Net;
using System.Text.RegularExpressions;
using Domain.Game;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Admin game oversight integration tests — verifies active game list and force-close via the MVC admin area.
/// Games are created directly in the DB for setup. Tests verify filter correctness and status transitions.
/// </summary>
public class AdminGamesTests : IntegrationTestBase
{
    private const string AdminEmail = "admin@admin.ee";
    private const string AdminPassword = "Admin1!";

    public AdminGamesTests(DatabaseFixture fixture) : base(fixture) { }

    // -------------------------------------------------------------------------
    // Test 1: GET /root/games returns 200 with active game lobby codes
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetGames_WithAdminCookie_Returns200ShowingLobbyGame()
    {
        // Seed a Lobby-status game
        var lobbyCode = "TST001";
        await SeedGameAsync(lobbyCode, EGameStatus.Lobby);

        var loginCookies = await LoginAndGetCookiesAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/root/games");
        foreach (var cookie in loginCookies)
            request.Headers.Add("Cookie", cookie);

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain(lobbyCode);
    }

    // -------------------------------------------------------------------------
    // Test 2: POST force-close transitions Lobby game to Completed
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostForceClose_OnLobbyGame_TransitionsToCompleted()
    {
        var lobbyCode = "TST002";
        var gameId = await SeedGameAsync(lobbyCode, EGameStatus.Lobby);

        var loginCookies = await LoginAndGetCookiesAsync();

        // GET the ForceClose confirmation page to extract the antiforgery token
        var (token, antiforgeryCookie) = await GetAntiForgeryTokenAsync($"/root/games/forceclose/{gameId}", loginCookies);
        token.ShouldNotBeNullOrEmpty("Could not extract antiforgery token from ForceClose page");

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/root/games/forceclose/{gameId}");
        request.Content = formContent;
        foreach (var cookie in loginCookies)
            request.Headers.Add("Cookie", cookie);
        if (!string.IsNullOrEmpty(antiforgeryCookie))
            request.Headers.Add("Cookie", antiforgeryCookie);

        var response = await Client.SendAsync(request);

        // Expect redirect to /root/games after successful force-close
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        // Verify game is now Completed in DB
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var game = await db.Games.AsNoTracking().FirstOrDefaultAsync(g => g.Id == gameId);
        game.ShouldNotBeNull();
        game.Status.ShouldBe(EGameStatus.Completed);
    }

    // -------------------------------------------------------------------------
    // Test 3: Completed games do NOT appear in active games list
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetGames_CompletedGameNotShown_InActiveList()
    {
        var lobbyCode = "TST003";
        await SeedGameAsync(lobbyCode, EGameStatus.Completed);

        var loginCookies = await LoginAndGetCookiesAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/root/games");
        foreach (var cookie in loginCookies)
            request.Headers.Add("Cookie", cookie);

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain(lobbyCode);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Seeds a game directly in the DB and returns its Id.
    /// </summary>
    private async Task<Guid> SeedGameAsync(string lobbyCode, EGameStatus status)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var game = new Game
        {
            LobbyCode = lobbyCode,
            Status = status,
            MaxPlayers = 4,
            TurnNumber = 0,
            MapWidth = 10,
            MapHeight = 10
        };

        db.Games.Add(game);
        await db.SaveChangesAsync();

        return game.Id;
    }

    /// <summary>
    /// Logs in as admin and returns ALL session cookies (antiforgery + auth session).
    /// Both sets are needed for subsequent POST requests that require ValidateAntiForgeryToken.
    /// </summary>
    private async Task<IEnumerable<string>> LoginAndGetCookiesAsync()
    {
        var getResponse = await Client.GetAsync("/root/account/login");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        token.ShouldNotBeNullOrEmpty("Could not find __RequestVerificationToken in login page HTML");

        // Capture the antiforgery cookie from the GET response — needed for POST validation
        var antiforgeryCookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0].Trim()).ToList()
            : new List<string>();

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", AdminEmail),
            new KeyValuePair<string, string>("Password", AdminPassword),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/root/account/login");
        loginRequest.Content = formContent;
        foreach (var cookie in antiforgeryCookies)
            loginRequest.Headers.Add("Cookie", cookie);

        var postResponse = await Client.SendAsync(loginRequest);

        // Return both the antiforgery cookies (GET) and session cookies (POST) so callers
        // can use this combined set for any subsequent POST that requires antiforgery validation.
        var sessionCookies = postResponse.Headers.Contains("Set-Cookie")
            ? postResponse.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0].Trim()).ToList()
            : new List<string>();

        return antiforgeryCookies.Concat(sessionCookies);
    }

    /// <summary>
    /// GETs a page with auth cookies to extract the antiforgery token and its cookie.
    /// </summary>
    private async Task<(string Token, string AntiforgeryCookie)> GetAntiForgeryTokenAsync(
        string url, IEnumerable<string> authCookies)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        foreach (var cookie in authCookies)
            request.Headers.Add("Cookie", cookie);

        var response = await Client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);

        var antiforgeryCookie = response.Headers.Contains("Set-Cookie")
            ? response.Headers.GetValues("Set-Cookie")
                .Select(c => c.Split(';')[0].Trim())
                .FirstOrDefault(c => c.Contains("Antiforgery") || c.Contains("RequestVerification")) ?? string.Empty
            : string.Empty;

        return (token, antiforgeryCookie);
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
