using System.Net;
using System.Text.RegularExpressions;
using Infrastructure.Seeding.Seeders;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Integration tests for admin reference data CRUD pages.
/// Verifies the generic base controller pattern works for FactionType and TerrainType.
/// Tests use the admin cookie login flow (same helper pattern as AdminAuthTests).
/// </summary>
public class AdminRefDataTests : IntegrationTestBase
{
    private const string AdminEmail = "admin@admin.ee";
    private const string AdminPassword = "Admin1!";

    public AdminRefDataTests(DatabaseFixture fixture) : base(fixture) { }

    // -------------------------------------------------------------------------
    // Test 1: GET FactionTypes index returns 200 with seeded faction names
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetFactionTypesIndex_WithAdminCookie_Returns200WithSeededData()
    {
        var cookies = await GetAdminCookiesAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/root/factiontypes");
        AddCookies(request, cookies);

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("Iron Throne", Case.Insensitive);
        html.ShouldContain("Faction Types", Case.Insensitive);
    }

    // -------------------------------------------------------------------------
    // Test 2: POST create a new FactionType redirects to Index
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostCreateFactionType_WithValidForm_RedirectsToIndex()
    {
        var cookies = await GetAdminCookiesAsync();

        // Get the Create page for the CSRF token
        var getRequest = new HttpRequestMessage(HttpMethod.Get, "/root/factiontypes/create");
        AddCookies(getRequest, cookies);
        var getResponse = await Client.SendAsync(getRequest);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        token.ShouldNotBeNullOrEmpty("Could not find __RequestVerificationToken on Create page");

        var antiforgCookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        // POST new faction type
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("NameEn", "Test Faction"),
            new KeyValuePair<string, string>("AttackModifier", "1.00"),
            new KeyValuePair<string, string>("HPModifier", "1.00"),
            new KeyValuePair<string, string>("BuildingCostModifier", "1.00"),
            new KeyValuePair<string, string>("ResourceProductionModifier", "1.00"),
            new KeyValuePair<string, string>("TrainingCostModifier", "1.00"),
            new KeyValuePair<string, string>("ActionPointModifier", "0"),
            new KeyValuePair<string, string>("HealRateModifier", "1.00"),
            new KeyValuePair<string, string>("StartingBonusAmount", "0"),
            new KeyValuePair<string, string>("Description", ""),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var postRequest = new HttpRequestMessage(HttpMethod.Post, "/root/factiontypes/create");
        postRequest.Content = formContent;
        AddCookies(postRequest, cookies);
        AddCookies(postRequest, antiforgCookies.Select(c => c.Split(';')[0].Trim()));

        var postResponse = await Client.SendAsync(postRequest);

        // Should redirect to Index (302)
        postResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        postResponse.Headers.Location!.ToString().ShouldContain("factiontypes", Case.Insensitive);
    }

    // -------------------------------------------------------------------------
    // Test 3: DELETE a seeded FactionType -- verify delete behavior
    //
    // In v6.0, FactionResourceBonus junction tables were removed.
    // FactionTypes may still be referenced by Kingdoms.
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteFactionType_SeededFaction_ShowsErrorOnIndex()
    {
        var cookies = await GetAdminCookiesAsync();

        // Get the Index to get an antiforgery token for the delete form
        var getRequest = new HttpRequestMessage(HttpMethod.Get, "/root/factiontypes");
        AddCookies(getRequest, cookies);
        var getResponse = await Client.SendAsync(getRequest);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        token.ShouldNotBeNullOrEmpty("Could not find __RequestVerificationToken on Index page");

        var antiforgCookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        // Attempt to delete Iron Throne — it may be referenced by ArmyTypes
        var ironThroneId = FactionTypeSeeder.IronThroneId;

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var deleteRequest = new HttpRequestMessage(HttpMethod.Post,
            $"/root/factiontypes/delete/{ironThroneId}");
        deleteRequest.Content = formContent;
        AddCookies(deleteRequest, cookies);
        AddCookies(deleteRequest, antiforgCookies.Select(c => c.Split(';')[0].Trim()));

        var deleteResponse = await Client.SendAsync(deleteRequest);

        // Should redirect back to Index (302) with TempData error set
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        // Follow redirect to Index and check for error message
        var indexRequest = new HttpRequestMessage(HttpMethod.Get,
            deleteResponse.Headers.Location!.ToString());
        AddCookies(indexRequest, cookies);
        var indexResponse = await Client.SendAsync(indexRequest);
        var indexHtml = await indexResponse.Content.ReadAsStringAsync();

        indexHtml.ShouldContain("Cannot delete", Case.Insensitive);
    }

    // -------------------------------------------------------------------------
    // Test 4: GET TerrainTypes index returns 200 — proves pattern works for other types
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTerrainTypesIndex_WithAdminCookie_Returns200()
    {
        var cookies = await GetAdminCookiesAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/root/terraintypes");
        AddCookies(request, cookies);

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("Terrain Types", Case.Insensitive);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Performs admin login and returns the session cookie values.
    /// </summary>
    private async Task<IEnumerable<string>> GetAdminCookiesAsync()
    {
        // Step 1: GET login page for antiforgery token
        var getResponse = await Client.GetAsync("/root/account/login");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        token.ShouldNotBeNullOrEmpty("Could not find __RequestVerificationToken in login page HTML");

        var antiforgCookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0].Trim())
            : Enumerable.Empty<string>();

        // Step 2: POST credentials
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", AdminEmail),
            new KeyValuePair<string, string>("Password", AdminPassword),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/root/account/login");
        loginRequest.Content = formContent;
        foreach (var cookie in antiforgCookies)
            loginRequest.Headers.Add("Cookie", cookie);

        var loginResponse = await Client.SendAsync(loginRequest);
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var sessionCookies = loginResponse.Headers.Contains("Set-Cookie")
            ? loginResponse.Headers.GetValues("Set-Cookie").Select(c => c.Split(';')[0].Trim())
            : Enumerable.Empty<string>();

        return antiforgCookies.Concat(sessionCookies).ToList();
    }

    private static void AddCookies(HttpRequestMessage request, IEnumerable<string> cookies)
    {
        foreach (var cookie in cookies)
            request.Headers.Add("Cookie", cookie);
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
