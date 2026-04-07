using System.Net;
using System.Net.Http.Json;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Phase 39 Public i18n Polish — Wave 0 scaffolded integration tests for MVCI18N-02.
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
///
/// All facts are Wave 0 stubs marked [Fact(Skip = ...)] — bodies are filled in
/// by plan 39-04 once the string extractions in 39-02/39-03 ship.
///
/// Helper pattern for setting culture cookie in HttpClient:
///   var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("et"));
///   Client.DefaultRequestHeaders.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}={cookieValue}");
/// </summary>
public class PublicViewLocalizationTests : IntegrationTestBase
{
    public PublicViewLocalizationTests(DatabaseFixture fixture) : base(fixture) { }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task LobbyIndex_WithEtCulture_RendersEstonianStrings()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task LobbyIndex_WithEnCulture_RendersEnglishStrings()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task LobbyDetail_WithEtCulture_RendersEstonianLabels()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task GameViews_WithEtCulture_RendersEstonianBuildingPanel()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task GameViews_WithEtCulture_RendersEstonianHudLabels()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 39-04")]
    public async Task RegisterPage_ConfirmPassword_LocalizedInBothCultures()
    {
        await Task.CompletedTask;
    }
}
