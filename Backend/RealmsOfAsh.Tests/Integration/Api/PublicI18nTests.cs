using System.Net;
using System.Net.Http.Json;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Phase 39 Public i18n Polish — Wave 0 scaffolded integration tests for MVCI18N-01.
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
///
/// All four facts are Wave 0 stubs marked [Fact(Skip = ...)] — bodies are filled
/// in by plan 39-04 once the underlying behavior ships in 39-01..39-03.
/// </summary>
public class PublicI18nTests : IntegrationTestBase
{
    public PublicI18nTests(DatabaseFixture fixture) : base(fixture) { }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task LanguageSwitcher_Renders_InPublicLayout_WithEnAndEt()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task SetLanguage_WithEnCulture_SetsCookie_AndRedirects()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task SetLanguage_WithEtCulture_SetsCookie_AndRedirects()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task PublicLayout_HtmlLangAttribute_ReflectsCurrentCulture()
    {
        await Task.CompletedTask;
    }
}
