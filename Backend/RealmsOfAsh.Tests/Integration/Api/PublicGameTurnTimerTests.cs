using System.Net;
using System.Text.RegularExpressions;
using Application.Contracts;
using Application.Services.GameInitialization;
using Application.Services.GameInitialization.DTOs.V1;
using Application.Services.Lobby;
using Application.Services.Lobby.DTOs.V1;
using Domain.Game;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Phase 38 — Public Live Turn Timer integration tests (MVCGAME-15).
/// Wave 1 (38-01) flips the Wave 0 stubs into real assertions.
/// </summary>
public class PublicGameTurnTimerTests : IntegrationTestBase
{
    private const string PublicCookieName = ".Thexomir.Public";

    public PublicGameTurnTimerTests(DatabaseFixture fixture) : base(fixture) { }

    // =========================================================================
    // MVCGAME-15: Live turn countdown timer
    // =========================================================================

    [Fact]
    public async Task MVCGAME_15_TurnTimer_RendersWhenDeadlineSet()
    {
        var ctx = await SeedActiveGameCtxAsync();
        var (client, cookie) = await CreateAuthenticatedClientForUserAsync(ctx.HostUserId);

        // Sanity: deadline IS set on a freshly seeded active game in the Action phase.
        var state = await GetGameStateAsync(ctx.GameId);
        state.TurnDeadline.ShouldNotBeNull("Freshly seeded active game must have TurnDeadline set");

        var response = await SendAuthedGetAsync(client, $"/Public/Game/Index/{ctx.GameId}", cookie);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.ShouldContain("id=\"turn-timer\"");
        html.ShouldContain("setInterval(tick, 1000)");
        html.ShouldContain("getElementById('turn-timer')");
    }

    [Fact]
    public async Task MVCGAME_15_TurnTimer_HiddenWhenNoDeadline()
    {
        var ctx = await SeedActiveGameCtxAsync();
        var (client, cookie) = await CreateAuthenticatedClientForUserAsync(ctx.HostUserId);

        // Force the deadline off via direct DbContext write.
        // Note: CustomWebApplicationFactory configures NoTrackingWithIdentityResolution as the
        // default, so a Find + property mutate + SaveChanges silently no-ops. ExecuteUpdateAsync
        // bypasses tracking entirely and issues a single UPDATE statement.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rows = await db.Games
                .Where(g => g.Id == ctx.GameId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(g => g.TurnDeadline, (DateTime?)null));
            rows.ShouldBe(1, "Expected exactly one Game row to be updated");
        }

        var response = await SendAuthedGetAsync(client, $"/Public/Game/Index/{ctx.GameId}", cookie);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        html.ShouldContain("id=\"turn-timer\"");      // span is unconditional
        html.ShouldNotContain("setInterval(tick, 1000)"); // script is Razor-guarded
    }

    // =========================================================================
    // Helpers (mirror PublicGameTests.cs — kept self-contained per plan guidance)
    // =========================================================================

    private sealed record GameTestContext(
        HttpClient Client,
        Guid GameId,
        string HostCookie,
        string Player2Cookie,
        Guid HostUserId,
        Guid Player2UserId);

    private async Task<GameTestContext> SeedActiveGameCtxAsync()
    {
        var (gameId, hostCookie, player2Cookie, hostUserId, player2UserId) = await SeedActiveGameAsync();
        return new GameTestContext(Client, gameId, hostCookie, player2Cookie, hostUserId, player2UserId);
    }

    private async Task<(Guid gameId, string hostCookie, string player2Cookie, Guid hostUserId, Guid player2UserId)>
        SeedActiveGameAsync()
    {
        var (hostEmail, _, hostCookie) = await RegisterAndExtractCookieAsync("host");
        var (player2Email, _, player2Cookie) = await RegisterAndExtractCookieAsync("player2");

        var hostUserId = await GetUserIdByEmailAsync(hostEmail);
        var player2UserId = await GetUserIdByEmailAsync(player2Email);

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
        var hostFactionId = factions[0].Id;
        var player2FactionId = factions[1].Id;

        var selectHost = await lobbyService.SelectFactionAsync(hostUserId, lobbyId, hostFactionId);
        selectHost.IsSuccess.ShouldBeTrue($"SelectFaction (host) failed: {selectHost.Error}");

        var selectPlayer2 = await lobbyService.SelectFactionAsync(player2UserId, lobbyId, player2FactionId);
        selectPlayer2.IsSuccess.ShouldBeTrue($"SelectFaction (player2) failed: {selectPlayer2.Error}");

        var startResult = await lobbyService.StartGameAsync(hostUserId, lobbyId);
        startResult.IsSuccess.ShouldBeTrue($"StartGame failed: {startResult.Error}");

        var initService = scope.ServiceProvider.GetRequiredService<IGameInitializationService>();
        var state = await initService.InitializeGameAsync(lobbyId);
        state.CurrentPhase.ShouldBe("Action", "Game should be in Action phase after initialization");
        state.Tiles.Count.ShouldBeGreaterThan(0, "Game should have tiles after initialization");

        return (lobbyId, hostCookie, player2Cookie, hostUserId, player2UserId);
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
        {
            request.Headers.Add("Cookie", cookie.Split(';')[0].Trim());
        }

        var postResponse = await Client.SendAsync(request);

        var setCookies = postResponse.Headers.Contains("Set-Cookie")
            ? postResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        return (postResponse.StatusCode, setCookies);
    }

    private async Task<(string email, string password, string publicCookieValue)> RegisterAndExtractCookieAsync(
        string usernameHint = "user")
    {
        var email = $"phase38-{usernameHint}-{Guid.NewGuid():N}@test.local";
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

    private async Task<(HttpClient client, string cookie)> CreateAuthenticatedClientForUserAsync(Guid userId)
    {
        var email = await GetEmailByUserIdAsync(userId);
        var (status, cookies) = await LoginAsync(email, "Player1!");
        status.ShouldBe(HttpStatusCode.Redirect);
        var publicCookie = cookies.FirstOrDefault(c => c.Contains(PublicCookieName));
        publicCookie.ShouldNotBeNullOrEmpty($"Expected {PublicCookieName} cookie after login");
        return (Client, publicCookie!.Split(';')[0].Trim());
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

    private async Task<HttpResponseMessage> SendAuthedGetAsync(HttpClient client, string url, string authCookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(authCookie))
            request.Headers.Add("Cookie", authCookie);
        return await client.SendAsync(request);
    }
}
