using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Admin auth coexistence tests — verifies that:
/// - Cookie challenge (302) works for unauthenticated admin area requests
/// - JWT challenge (401) is preserved for unauthenticated API requests
/// - Admin login flow works end-to-end
/// - Non-admin users are rejected at login
/// </summary>
public class AdminAuthTests : IntegrationTestBase
{
    private const string AdminEmail = "admin@admin.ee";
    private const string AdminPassword = "Admin1!";
    private const string PlayerEmail = "player1@player.ee";
    private const string PlayerPassword = "Player1!";

    public AdminAuthTests(DatabaseFixture fixture) : base(fixture) { }

    // -------------------------------------------------------------------------
    // Test 1: Unauthenticated GET /root/home returns 302 redirect to login
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAdminHome_WithoutCookie_Returns302RedirectToLogin()
    {
        var response = await Client.GetAsync("/root/home");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString().ShouldContain("/root/account/login",
            Case.Insensitive);
    }

    // -------------------------------------------------------------------------
    // Test 2: Unauthenticated GET /api/v1/lobby still returns 401 (not 302)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetApiLobby_WithoutToken_Returns401NotRedirect()
    {
        var response = await Client.GetAsync("/api/v1/lobby");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Test 3: Admin login sets cookie and redirects to /root/home
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostAdminLogin_WithValidAdminCredentials_SetsCookieAndRedirects()
    {
        var (statusCode, cookies) = await LoginAsync(AdminEmail, AdminPassword);

        statusCode.ShouldBe(HttpStatusCode.Redirect);
        cookies.ShouldContain(c => c.Contains(".Thexomir.Admin"),
            "Expected admin cookie to be set after login");
    }

    // -------------------------------------------------------------------------
    // Test 4: Admin cookie allows access to /root/home (200)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAdminHome_WithValidAdminCookie_Returns200()
    {
        var (_, cookies) = await LoginAsync(AdminEmail, AdminPassword);

        // Use the admin cookie for the next request
        var request = new HttpRequestMessage(HttpMethod.Get, "/root/home");
        foreach (var cookie in cookies)
        {
            request.Headers.Add("Cookie", cookie);
        }

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // Test 5: Non-admin login is rejected with "Not an admin account." error
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostAdminLogin_WithNonAdminCredentials_ReturnsLoginPageWithError()
    {
        var (statusCode, _) = await LoginAsync(PlayerEmail, PlayerPassword);

        // A rejected login returns the login page (200), not a redirect
        statusCode.ShouldBe(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Performs the admin cookie login flow:
    /// 1. GET /root/account/login to retrieve the antiforgery token
    /// 2. POST credentials + antiforgery token
    /// Returns the POST response status code and all Set-Cookie header values.
    /// </summary>
    private async Task<(HttpStatusCode StatusCode, IEnumerable<string> Cookies)> LoginAsync(
        string email, string password)
    {
        // Step 1: GET the login page to extract the antiforgery token
        var getResponse = await Client.GetAsync("/root/account/login");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        token.ShouldNotBeNullOrEmpty("Could not find __RequestVerificationToken in login page HTML");

        // Collect antiforgery cookie from GET response
        var antiforgeryCoookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        // Step 2: POST credentials with antiforgery token
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/root/account/login");
        request.Content = formContent;

        // Forward antiforgery cookie(s) from the GET response
        foreach (var cookie in antiforgeryCoookies)
        {
            // Extract just the name=value part (before the first semicolon)
            var cookieValue = cookie.Split(';')[0].Trim();
            request.Headers.Add("Cookie", cookieValue);
        }

        var postResponse = await Client.SendAsync(request);

        var setCookies = postResponse.Headers.Contains("Set-Cookie")
            ? postResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        return (postResponse.StatusCode, setCookies);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(html,
            @"<input[^>]+name=""__RequestVerificationToken""[^>]+value=""([^""]+)""",
            RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value;

        // Try alternate attribute order
        match = Regex.Match(html,
            @"<input[^>]+value=""([^""]+)""[^>]+name=""__RequestVerificationToken""",
            RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
