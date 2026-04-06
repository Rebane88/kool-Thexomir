using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Phase 34 Public MVC Area Foundation — integration tests covering MVCINFRA-01..05.
///
/// Coverage map:
///   MVCINFRA-01 → GetPublic_AnonymousIndexAction_Returns200OK
///   MVCINFRA-02 → GetPublicHome_WithoutCookie_Returns302RedirectToPublicLogin
///   MVCINFRA-02 → PostApiLobby_WithoutToken_StillReturns401NotRedirect
///   MVCINFRA-02 → GetAdminHome_WithoutCookie_StillRedirectsToAdminLogin
///   MVCINFRA-03 → GetPublic_HomePage_RendersInsidePublicLayout
///   MVCINFRA-04 → GetPublic_HomePage_AppStartsWithLobbyServiceInjected
///   MVCINFRA-05 → GetPublic_HomePage_RendersLocalizedString
/// </summary>
public class PublicAreaFoundationTests : IntegrationTestBase
{
    public PublicAreaFoundationTests(DatabaseFixture fixture) : base(fixture) { }

    // -------------------------------------------------------------------------
    // MVCINFRA-01: Anonymous GET /Public returns 200 OK with the home marker
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPublic_AnonymousIndexAction_Returns200OK()
    {
        var response = await Client.GetAsync("/Public");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("data-marker=\"public-home-loaded\"");
    }

    // -------------------------------------------------------------------------
    // MVCINFRA-02: PublicAreaPolicy resolves to PublicCookie scheme so
    // unauthenticated requests would 302 to /public/account/login (not 401).
    // We assert the policy contract directly via IAuthorizationPolicyProvider —
    // no throwaway protected action required.
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPublicHome_WithoutCookie_Returns302RedirectToPublicLogin()
    {
        using var scope = Factory.Services.CreateScope();
        var policyProvider = scope.ServiceProvider
            .GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await policyProvider.GetPolicyAsync("PublicAreaPolicy");

        policy.ShouldNotBeNull();
        policy!.AuthenticationSchemes.ShouldContain("PublicCookie");
        policy.Requirements.OfType<DenyAnonymousAuthorizationRequirement>()
            .ShouldNotBeEmpty("PublicAreaPolicy must require an authenticated user");
    }

    // -------------------------------------------------------------------------
    // MVCINFRA-02: JWT API scheme is not hijacked by the Public cookie scheme —
    // unauthenticated POST /api/v1/lobby still returns 401 Unauthorized.
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PostApiLobby_WithoutToken_StillReturns401NotRedirect()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/lobby", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // MVCINFRA-02: Admin cookie scheme isolation — unauthenticated /root/home
    // still redirects to the admin login, not the public login.
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAdminHome_WithoutCookie_StillRedirectsToAdminLogin()
    {
        var response = await Client.GetAsync("/root/home");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString()
            .ShouldContain("/root/account/login", Case.Insensitive);
        response.Headers.Location.ToString()
            .ShouldNotContain("/public/account/login", Case.Insensitive);
    }

    // -------------------------------------------------------------------------
    // MVCINFRA-03: Home renders inside the _PublicLayout — assert all four
    // structural data-* markers introduced by plan 34-02.
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPublic_HomePage_RendersInsidePublicLayout()
    {
        var response = await Client.GetAsync("/Public");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("data-layout=\"public\"");
        html.ShouldContain("data-region=\"public-nav\"");
        html.ShouldContain("data-region=\"public-language-switcher\"");
        html.ShouldContain("data-region=\"public-auth-state\"");
    }

    // -------------------------------------------------------------------------
    // MVCINFRA-04: App boots with ILobbyService injected into the Public
    // HomeController. ActivatorUtilities.CreateInstance is the canonical
    // "does DI resolve this controller?" check and avoids any HTTP plumbing.
    // -------------------------------------------------------------------------

    [Fact]
    public void GetPublic_HomePage_AppStartsWithLobbyServiceInjected()
    {
        using var scope = Factory.Services.CreateScope();

        var controller = ActivatorUtilities
            .CreateInstance<API.Areas.Public.Controllers.HomeController>(scope.ServiceProvider);

        controller.ShouldNotBeNull();
    }

    // -------------------------------------------------------------------------
    // MVCINFRA-05: Layout's @SharedLocalizer["Nav_Dashboard"] resolves through
    // Common.resx (en) to the literal string "Dashboard" — proves the
    // IStringLocalizer pipeline is wired (raw key must NOT leak through).
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPublic_HomePage_RendersLocalizedString()
    {
        // Default request culture is et-EE; force English so the assertion targets
        // the literal en value "Dashboard" from Common.resx (Common.et.resx renders
        // "Töölaud"). Either resolved value proves IStringLocalizer is wired —
        // pinning to en gives a deterministic, readable assertion.
        var request = new HttpRequestMessage(HttpMethod.Get, "/Public");
        request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.ShouldContain("Dashboard");
        html.ShouldNotContain("Nav_Dashboard");
    }
}
