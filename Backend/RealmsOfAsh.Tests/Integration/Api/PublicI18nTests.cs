using System.Net;
using Microsoft.AspNetCore.Localization;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Phase 39 Public i18n Polish — integration tests for MVCI18N-01 (language switcher + culture cookie + html lang).
///
/// MVCI18N-01: Public area exposes a working language switcher that persists the
/// chosen culture via the ASP.NET Core CookieRequestCultureProvider cookie
/// (.AspNetCore.Culture) and reflects the active culture in the rendered HTML.
///
/// Coverage map:
///   MVCI18N-01 (switcher renders)        → LanguageSwitcher_Renders_InPublicLayout_WithEnAndEt
///   MVCI18N-01 (set en sets cookie)      → SetLanguage_WithEnCulture_SetsCookie_AndRedirects
///   MVCI18N-01 (set et sets cookie)      → SetLanguage_WithEtCulture_SetsCookie_AndRedirects
///   MVCI18N-01 (html lang reflects culture) → PublicLayout_HtmlLangAttribute_ReflectsCurrentCulture
/// </summary>
public class PublicI18nTests : IntegrationTestBase
{
    public PublicI18nTests(DatabaseFixture fixture) : base(fixture) { }

    [Fact]
    public async Task LanguageSwitcher_Renders_InPublicLayout_WithEnAndEt()
    {
        var response = await Client.GetAsync("/Public");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("data-region=\"public-language-switcher\"");
        // The partial renders anchor tags with asp-route-culture=en/et which emit ?culture=en / ?culture=et
        html.ShouldContain("culture=en");
        html.ShouldContain("culture=et");
    }

    [Fact]
    public async Task SetLanguage_WithEnCulture_SetsCookie_AndRedirects()
    {
        var response = await Client.GetAsync("/Home/SetLanguage?culture=en&returnUrl=%2FPublic");

        // Either 302 Redirect or 302 Found — both are valid redirects
        new[] { HttpStatusCode.Redirect, HttpStatusCode.Found }.ShouldContain(response.StatusCode);

        response.Headers.Contains("Set-Cookie").ShouldBeTrue(
            "SetLanguage should emit a Set-Cookie header for .AspNetCore.Culture");
        var setCookies = response.Headers.GetValues("Set-Cookie").ToList();
        setCookies.ShouldContain(
            c => c.Contains(CookieRequestCultureProvider.DefaultCookieName)
                 && (c.Contains("c=en") || c.Contains("c%3Den")),
            $"Expected a Set-Cookie containing {CookieRequestCultureProvider.DefaultCookieName}=...c=en...");
    }

    [Fact]
    public async Task SetLanguage_WithEtCulture_SetsCookie_AndRedirects()
    {
        var response = await Client.GetAsync("/Home/SetLanguage?culture=et&returnUrl=%2FPublic");

        new[] { HttpStatusCode.Redirect, HttpStatusCode.Found }.ShouldContain(response.StatusCode);

        response.Headers.Contains("Set-Cookie").ShouldBeTrue(
            "SetLanguage should emit a Set-Cookie header for .AspNetCore.Culture");
        var setCookies = response.Headers.GetValues("Set-Cookie").ToList();
        setCookies.ShouldContain(
            c => c.Contains(CookieRequestCultureProvider.DefaultCookieName)
                 && (c.Contains("c=et") || c.Contains("c%3Det")),
            $"Expected a Set-Cookie containing {CookieRequestCultureProvider.DefaultCookieName}=...c=et...");
    }

    [Fact]
    public async Task PublicLayout_HtmlLangAttribute_ReflectsCurrentCulture()
    {
        SetCultureCookieOnDefaultClient("et");

        var response = await Client.GetAsync("/Public");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("<html lang=\"et\"");
    }

    /// <summary>
    /// Sets the .AspNetCore.Culture cookie on the shared Client so subsequent requests
    /// render under the given UI culture. Mutates Client.DefaultRequestHeaders.
    /// </summary>
    private void SetCultureCookieOnDefaultClient(string culture)
    {
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add(
            "Cookie",
            $"{CookieRequestCultureProvider.DefaultCookieName}={cookieValue}");
    }
}
