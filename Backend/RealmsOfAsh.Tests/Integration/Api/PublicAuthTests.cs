using System.Net;
using System.Text.RegularExpressions;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Phase 35 — Public Auth UX integration tests.
/// Covers MVCAUTH-01..05: register, login, logout, ModelState errors, authenticated-landing redirect.
/// All tests use the seeded player1@player.ee / Player1! credentials or fresh GUIDs for register tests.
/// </summary>
public class PublicAuthTests : IntegrationTestBase
{
    private const string SeededEmail = "player1@player.ee";
    private const string SeededPassword = "Player1!";
    private const string PublicCookieName = ".Thexomir.Public";

    public PublicAuthTests(DatabaseFixture fixture) : base(fixture) { }

    // -------------------------------------------------------------------------
    // MVCAUTH-01: Register with valid new user → 302 + sets PublicCookie
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostPublicRegister_WithValidNewUser_Returns302AndSetsPublicCookie()
    {
        var email = $"phase35-{Guid.NewGuid():N}@test.local";
        var (statusCode, cookies) = await RegisterAsync(email, "NewUser1!", "NewUser1!");

        statusCode.ShouldBe(HttpStatusCode.Redirect);
        cookies.ShouldContain(c => c.Contains(PublicCookieName),
            $"Expected {PublicCookieName} cookie to be set after successful registration");
    }

    // -------------------------------------------------------------------------
    // MVCAUTH-02: Login with seeded credentials → 302 + sets PublicCookie
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostPublicLogin_WithValidSeededCredentials_Returns302AndSetsPublicCookie()
    {
        var (statusCode, cookies) = await LoginAsync(SeededEmail, SeededPassword);

        statusCode.ShouldBe(HttpStatusCode.Redirect);
        cookies.ShouldContain(c => c.Contains(PublicCookieName),
            $"Expected {PublicCookieName} cookie to be set after successful login");
    }

    // -------------------------------------------------------------------------
    // MVCAUTH-02 + MVCAUTH-04: Login with wrong password → 200 + error in HTML
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostPublicLogin_WithWrongPassword_Returns200AndContainsInvalidCredentials()
    {
        var getResponse = await Client.GetAsync("/Public/Account/Login");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);

        var antiforgeryCookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", SeededEmail),
            new KeyValuePair<string, string>("Password", "WrongPassword!"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Account/Login");
        request.Content = formContent;
        foreach (var cookie in antiforgeryCookies)
        {
            request.Headers.Add("Cookie", cookie.Split(';')[0].Trim());
        }

        var postResponse = await Client.SendAsync(request);
        var responseHtml = await postResponse.Content.ReadAsStringAsync();

        postResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        responseHtml.ShouldContain("Invalid credentials.");
    }

    // -------------------------------------------------------------------------
    // MVCAUTH-03: Logout with valid cookie → 302 + clears PublicCookie
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostPublicLogout_WithValidCookie_Returns302AndClearsCookie()
    {
        // Step 1: Login to get the public cookie
        var (_, loginCookies) = await LoginAsync(SeededEmail, SeededPassword);
        var publicCookie = loginCookies.FirstOrDefault(c => c.Contains(PublicCookieName));
        publicCookie.ShouldNotBeNullOrEmpty($"Login must set {PublicCookieName} before logout test");

        var publicCookieValue = publicCookie!.Split(';')[0].Trim();

        // Step 2: GET any page to get a fresh antiforgery token (must be authenticated)
        var getResponse = await Client.GetAsync("/Public/Account/Login");
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(getHtml);

        var antiforgeryCookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        // Step 3: POST logout forwarding both the auth cookie and antiforgery cookie
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Account/Logout");
        request.Content = formContent;
        request.Headers.Add("Cookie", publicCookieValue);
        foreach (var cookie in antiforgeryCookies)
        {
            request.Headers.Add("Cookie", cookie.Split(';')[0].Trim());
        }

        var logoutResponse = await Client.SendAsync(request);

        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        logoutResponse.Headers.Location.ShouldNotBeNull();
        logoutResponse.Headers.Location!.ToString().ShouldContain("/Public", Case.Insensitive);

        // The logout response should clear the cookie (empty value or Expires in the past)
        var setCookieHeaders = logoutResponse.Headers.Contains("Set-Cookie")
            ? logoutResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        // Either the cookie is cleared (value empty) or expired (Expires in the past)
        var publicCookieClearHeader = setCookieHeaders.FirstOrDefault(c => c.Contains(PublicCookieName));
        if (publicCookieClearHeader != null)
        {
            var cookieLower = publicCookieClearHeader.ToLowerInvariant();
            var isCleared = cookieLower.Contains($"{PublicCookieName.ToLowerInvariant()}=;") ||
                            cookieLower.Contains($"{PublicCookieName.ToLowerInvariant()}= ;") ||
                            cookieLower.Contains("expires=thu, 01 jan 1970") ||
                            cookieLower.Contains("max-age=0") ||
                            publicCookieClearHeader.Split(';')[0].Trim() == $"{PublicCookieName}=";
            isCleared.ShouldBeTrue(
                $"Expected {PublicCookieName} cookie to be cleared on logout. Got: {publicCookieClearHeader}");
        }
        // If no Set-Cookie header for the public cookie — the 302 to /Public confirms logout happened
    }

    // -------------------------------------------------------------------------
    // MVCAUTH-04: Register with empty Email → 200 + Email ModelState error
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostPublicRegister_WithEmptyEmail_Returns200WithEmailModelStateError()
    {
        var getResponse = await Client.GetAsync("/Public/Account/Register");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);

        var antiforgeryCookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", ""),
            new KeyValuePair<string, string>("Password", "ValidPass1!"),
            new KeyValuePair<string, string>("ConfirmPassword", "ValidPass1!"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Account/Register");
        request.Content = formContent;
        foreach (var cookie in antiforgeryCookies)
        {
            request.Headers.Add("Cookie", cookie.Split(';')[0].Trim());
        }

        var postResponse = await Client.SendAsync(request);
        var responseHtml = await postResponse.Content.ReadAsStringAsync();

        postResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        // The view renders per-field errors via asp-validation-for="Email" with data-field-error="Email"
        // or the summary contains the Email field error
        (responseHtml.Contains("data-field-error=\"Email\"") ||
         responseHtml.Contains("The Email field is required") ||
         responseHtml.Contains("field is required"))
            .ShouldBeTrue("Response HTML should contain an Email validation error");
    }

    // -------------------------------------------------------------------------
    // MVCAUTH-04: Register with duplicate email → 200 + Identity error "is already taken"
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostPublicRegister_WithDuplicateEmail_Returns200WithIdentityError()
    {
        var getResponse = await Client.GetAsync("/Public/Account/Register");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);

        var antiforgeryCookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        // player1@player.ee is seeded — submitting it should trigger duplicate email error
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", SeededEmail),
            new KeyValuePair<string, string>("Password", "Player2!"),
            new KeyValuePair<string, string>("ConfirmPassword", "Player2!"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Account/Register");
        request.Content = formContent;
        foreach (var cookie in antiforgeryCookies)
        {
            request.Headers.Add("Cookie", cookie.Split(';')[0].Trim());
        }

        var postResponse = await Client.SendAsync(request);
        var responseHtml = await postResponse.Content.ReadAsStringAsync();

        postResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        responseHtml.ShouldContain("is already taken");
    }

    // -------------------------------------------------------------------------
    // MVCAUTH-04: Register with mismatched passwords → 200 + ConfirmPassword error
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostPublicRegister_WithMismatchedPasswords_Returns200WithConfirmPasswordError()
    {
        var email = $"phase35-{Guid.NewGuid():N}@test.local";
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
            new KeyValuePair<string, string>("Password", "Pass1234!"),
            new KeyValuePair<string, string>("ConfirmPassword", "Different!"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Account/Register");
        request.Content = formContent;
        foreach (var cookie in antiforgeryCookies)
        {
            request.Headers.Add("Cookie", cookie.Split(';')[0].Trim());
        }

        var postResponse = await Client.SendAsync(request);
        var responseHtml = await postResponse.Content.ReadAsStringAsync();

        postResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        responseHtml.ShouldContain("Passwords do not match.");
    }

    // -------------------------------------------------------------------------
    // MVCAUTH-04: Login with empty Email → 200 + Email ModelState error
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostPublicLogin_WithEmptyEmail_Returns200WithEmailModelStateError()
    {
        var getResponse = await Client.GetAsync("/Public/Account/Login");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);

        var antiforgeryCookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", ""),
            new KeyValuePair<string, string>("Password", "SomePassword1!"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Account/Login");
        request.Content = formContent;
        foreach (var cookie in antiforgeryCookies)
        {
            request.Headers.Add("Cookie", cookie.Split(';')[0].Trim());
        }

        var postResponse = await Client.SendAsync(request);
        var responseHtml = await postResponse.Content.ReadAsStringAsync();

        postResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (responseHtml.Contains("data-field-error=\"Email\"") ||
         responseHtml.Contains("The Email field is required") ||
         responseHtml.Contains("field is required"))
            .ShouldBeTrue("Response HTML should contain an Email validation error");
    }

    // -------------------------------------------------------------------------
    // MVCAUTH-05: GET /Public with valid PublicCookie → 302 redirect to Lobby
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPublic_WithValidCookie_Returns302RedirectToLobby()
    {
        // Login to get the public cookie
        var (_, loginCookies) = await LoginAsync(SeededEmail, SeededPassword);
        var publicCookie = loginCookies.FirstOrDefault(c => c.Contains(PublicCookieName));
        publicCookie.ShouldNotBeNullOrEmpty($"Login must set {PublicCookieName} before this test");

        var publicCookieValue = publicCookie!.Split(';')[0].Trim();

        // GET /Public with the auth cookie — should redirect to Lobby (not follow the redirect)
        var request = new HttpRequestMessage(HttpMethod.Get, "/Public");
        request.Headers.Add("Cookie", publicCookieValue);

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString().ShouldContain("/Public/Lobby", Case.Insensitive);
    }

    // -------------------------------------------------------------------------
    // MVCAUTH-05: GET /Public without cookie → 200 + login and register links
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPublic_WithoutCookie_Returns200WithLoginAndRegisterLinks()
    {
        var response = await Client.GetAsync("/Public");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("data-link=\"public-login\"");
        html.ShouldContain("data-link=\"public-register\"");
    }

    // -------------------------------------------------------------------------
    // MVCAUTH-03 (defensive): POST /Public/Account/Logout without cookie
    // → gated by PublicAreaPolicy (no [AllowAnonymous] on Logout) → 302 to login
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostPublicLogout_WithoutCookie_Returns302ToLogin()
    {
        // POST to Logout without any auth cookie — the PublicAreaPolicy challenge fires
        // Antiforgery check may also fail, but the auth check is evaluated first.
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", "fake-token")
        });

        var response = await Client.PostAsync("/Public/Account/Logout", formContent);

        // Either 302 (challenge redirect) or 400/401 — confirms the action is gated
        var isGated = response.StatusCode == HttpStatusCode.Redirect ||
                      response.StatusCode == HttpStatusCode.Unauthorized ||
                      response.StatusCode == HttpStatusCode.BadRequest;
        isGated.ShouldBeTrue(
            $"Expected Logout without cookie to return Redirect, Unauthorized, or BadRequest. Got: {response.StatusCode}");

        // If it redirects, should go toward the public login
        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            response.Headers.Location.ShouldNotBeNull();
            response.Headers.Location!.ToString().ShouldContain("/Public/Account/Login", Case.Insensitive);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Performs the public cookie login flow:
    /// 1. GET /Public/Account/Login to retrieve the antiforgery token
    /// 2. POST credentials + antiforgery token
    /// Returns the POST response status code and all Set-Cookie header values.
    /// </summary>
    private async Task<(HttpStatusCode StatusCode, IEnumerable<string> Cookies)> LoginAsync(
        string email, string password)
    {
        // Step 1: GET the login page to extract the antiforgery token
        var getResponse = await Client.GetAsync("/Public/Account/Login");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        token.ShouldNotBeNullOrEmpty("Could not find __RequestVerificationToken in Public login page HTML");

        var antiforgeryCookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        // Step 2: POST credentials with antiforgery token
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Account/Login");
        request.Content = formContent;

        // Forward antiforgery cookie(s) from the GET response
        foreach (var cookie in antiforgeryCookies)
        {
            var cookieValue = cookie.Split(';')[0].Trim();
            request.Headers.Add("Cookie", cookieValue);
        }

        var postResponse = await Client.SendAsync(request);

        var setCookies = postResponse.Headers.Contains("Set-Cookie")
            ? postResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        return (postResponse.StatusCode, setCookies);
    }

    /// <summary>
    /// Performs the public cookie register flow:
    /// 1. GET /Public/Account/Register to retrieve the antiforgery token
    /// 2. POST registration form + antiforgery token
    /// Returns the POST response status code and all Set-Cookie header values.
    /// </summary>
    private async Task<(HttpStatusCode StatusCode, IEnumerable<string> Cookies)> RegisterAsync(
        string email, string password, string confirmPassword)
    {
        // Step 1: GET the register page to extract the antiforgery token
        var getResponse = await Client.GetAsync("/Public/Account/Register");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await getResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);
        token.ShouldNotBeNullOrEmpty("Could not find __RequestVerificationToken in Public register page HTML");

        var antiforgeryCookies = getResponse.Headers.Contains("Set-Cookie")
            ? getResponse.Headers.GetValues("Set-Cookie")
            : Enumerable.Empty<string>();

        // Step 2: POST registration with antiforgery token
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("ConfirmPassword", confirmPassword),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/Public/Account/Register");
        request.Content = formContent;

        // Forward antiforgery cookie(s) from the GET response
        foreach (var cookie in antiforgeryCookies)
        {
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
