using System.Net;
using System.Text.RegularExpressions;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Admin user management integration tests — verifies user list, lock, and unlock via the MVC admin area.
/// Each test logs in as admin@admin.ee and exercises POST actions with antiforgery tokens.
/// </summary>
public class AdminUsersTests : IntegrationTestBase
{
    private const string AdminEmail = "admin@admin.ee";
    private const string AdminPassword = "Admin1!";
    private const string PlayerEmail = "player1@player.ee";

    public AdminUsersTests(DatabaseFixture fixture) : base(fixture) { }

    // -------------------------------------------------------------------------
    // Test 1: GET /root/users returns 200 with user list
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUsers_WithAdminCookie_Returns200WithUserList()
    {
        var cookies = await LoginAndGetCookiesAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/root/users");
        foreach (var cookie in cookies)
            request.Headers.Add("Cookie", cookie);

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain(AdminEmail);
    }

    // -------------------------------------------------------------------------
    // Test 2: POST lock user sets LockoutEnd to far-future date
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostLockUser_WithAdminCookie_SetsLockoutEnd()
    {
        var loginCookies = await LoginAndGetCookiesAsync();

        // Resolve UserManager to find the player user ID
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var player = await userManager.FindByEmailAsync(PlayerEmail);
        player.ShouldNotBeNull();

        // GET the users page to extract antiforgery token + cookie
        var (token, antiforgeryCookie) = await GetAntiForgeryTokenAsync("/root/users", loginCookies);

        // POST lock user
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("id", player.Id.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/root/users/lockuser/{player.Id}");
        request.Content = formContent;
        foreach (var cookie in loginCookies)
            request.Headers.Add("Cookie", cookie);
        if (!string.IsNullOrEmpty(antiforgeryCookie))
            request.Headers.Add("Cookie", antiforgeryCookie);

        var response = await Client.SendAsync(request);

        // Expect redirect after successful lock
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        // Verify the user is now locked in the DB
        using var verifyScope = Factory.Services.CreateScope();
        var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var lockedPlayer = await verifyUserManager.FindByEmailAsync(PlayerEmail);
        lockedPlayer.ShouldNotBeNull();
        lockedPlayer.LockoutEnd.ShouldNotBeNull();
        lockedPlayer.LockoutEnd!.Value.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddYears(100));
    }

    // -------------------------------------------------------------------------
    // Test 3: POST unlock previously locked user clears LockoutEnd
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostUnlockUser_AfterLocking_ClearsLockoutEnd()
    {
        var loginCookies = await LoginAndGetCookiesAsync();

        // Find player
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var player = await userManager.FindByEmailAsync(PlayerEmail);
        player.ShouldNotBeNull();

        // Lock the user first via the endpoint
        var (lockToken, lockAntiforgery) = await GetAntiForgeryTokenAsync("/root/users", loginCookies);
        var lockContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("id", player.Id.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", lockToken)
        });
        var lockRequest = new HttpRequestMessage(HttpMethod.Post, $"/root/users/lockuser/{player.Id}");
        lockRequest.Content = lockContent;
        foreach (var cookie in loginCookies)
            lockRequest.Headers.Add("Cookie", cookie);
        if (!string.IsNullOrEmpty(lockAntiforgery))
            lockRequest.Headers.Add("Cookie", lockAntiforgery);
        await Client.SendAsync(lockRequest);

        // Now unlock
        var (unlockToken, unlockAntiforgery) = await GetAntiForgeryTokenAsync("/root/users", loginCookies);
        var unlockContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("id", player.Id.ToString()),
            new KeyValuePair<string, string>("__RequestVerificationToken", unlockToken)
        });
        var unlockRequest = new HttpRequestMessage(HttpMethod.Post, $"/root/users/unlockuser/{player.Id}");
        unlockRequest.Content = unlockContent;
        foreach (var cookie in loginCookies)
            unlockRequest.Headers.Add("Cookie", cookie);
        if (!string.IsNullOrEmpty(unlockAntiforgery))
            unlockRequest.Headers.Add("Cookie", unlockAntiforgery);

        var response = await Client.SendAsync(unlockRequest);
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        // Verify LockoutEnd is cleared
        using var verifyScope = Factory.Services.CreateScope();
        var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var unlockedPlayer = await verifyUserManager.FindByEmailAsync(PlayerEmail);
        unlockedPlayer.ShouldNotBeNull();
        var isLocked = unlockedPlayer.LockoutEnd.HasValue && unlockedPlayer.LockoutEnd > DateTimeOffset.UtcNow;
        isLocked.ShouldBeFalse("User should not be locked after unlock");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Logs in as admin and returns ALL session cookies (antiforgery + auth session).
    /// Both sets are needed for subsequent POST requests that require ValidateAntiForgeryToken.
    /// </summary>
    private async Task<IEnumerable<string>> LoginAndGetCookiesAsync()
    {
        // GET login page for antiforgery token
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

        var request = new HttpRequestMessage(HttpMethod.Post, "/root/account/login");
        request.Content = formContent;
        foreach (var cookie in antiforgeryCookies)
            request.Headers.Add("Cookie", cookie);

        var postResponse = await Client.SendAsync(request);

        // Return both the antiforgery cookies (GET) and session cookies (POST) so callers
        // can use this combined set for any subsequent POST that requires antiforgery validation.
        var sessionCookies = postResponse.Headers.Contains("Set-Cookie")
            ? postResponse.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0].Trim()).ToList()
            : new List<string>();

        return antiforgeryCookies.Concat(sessionCookies);
    }

    /// <summary>
    /// GETs a page with the given auth cookies to extract the antiforgery token and its cookie.
    /// Returns (token value, antiforgery cookie name=value string).
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

        // Also pick up any antiforgery cookie from this GET response
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
