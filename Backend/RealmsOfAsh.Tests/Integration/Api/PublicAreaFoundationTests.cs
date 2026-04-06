using System.Net;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Phase 34 Public MVC Area Foundation — Wave 0 stubs.
/// Each [Fact] is currently Skip'd; plan 34-03 fills in real assertions
/// once the Public area, layout, and home view exist (delivered by plan 34-02).
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

    [Fact(Skip = "Wave 0 stub — implemented in plan 34-03")]
    public Task GetPublic_AnonymousIndexAction_Returns200OK() => Task.CompletedTask;

    [Fact(Skip = "Wave 0 stub — implemented in plan 34-03")]
    public Task GetPublicHome_WithoutCookie_Returns302RedirectToPublicLogin() => Task.CompletedTask;

    [Fact(Skip = "Wave 0 stub — implemented in plan 34-03")]
    public Task PostApiLobby_WithoutToken_StillReturns401NotRedirect() => Task.CompletedTask;

    [Fact(Skip = "Wave 0 stub — implemented in plan 34-03")]
    public Task GetAdminHome_WithoutCookie_StillRedirectsToAdminLogin() => Task.CompletedTask;

    [Fact(Skip = "Wave 0 stub — implemented in plan 34-03")]
    public Task GetPublic_HomePage_RendersInsidePublicLayout() => Task.CompletedTask;

    [Fact(Skip = "Wave 0 stub — implemented in plan 34-03")]
    public Task GetPublic_HomePage_AppStartsWithLobbyServiceInjected() => Task.CompletedTask;

    [Fact(Skip = "Wave 0 stub — implemented in plan 34-03")]
    public Task GetPublic_HomePage_RendersLocalizedString() => Task.CompletedTask;
}
