using RealmsOfAsh.Tests.Fixtures;

namespace RealmsOfAsh.Tests.Integration.Api;

/// <summary>
/// Phase 36 — Public Lobby UX & Real-Time integration tests (Wave 0 stubs).
/// Covers MVCLOBBY-01..07 and MVCRT-01.
/// Every method is a skipped stub that Plan 36-06 will implement. The file exists
/// so that subsequent plans in Phase 36 can reference these test names in their
/// &lt;verify&gt; blocks from Wave 2 onward (Nyquist gate).
/// </summary>
public class PublicLobbyTests : IntegrationTestBase
{
    public PublicLobbyTests(DatabaseFixture fixture) : base(fixture) { }

    // -------------------------------------------------------------------------
    // MVCLOBBY-01: Lobby index — authenticated sees list, anonymous redirects
    // -------------------------------------------------------------------------

    [Fact(Skip = "Wave 0 stub - implemented in 36-06-PLAN.md")]
    public async Task LobbyIndex_Authenticated_Returns200WithLobbyList()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 36-06-PLAN.md")]
    public async Task LobbyIndex_Unauthenticated_Returns302ToLogin()
    {
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // MVCLOBBY-02: Create lobby — valid data redirects, invalid shows error
    // -------------------------------------------------------------------------

    [Fact(Skip = "Wave 0 stub - implemented in 36-06-PLAN.md")]
    public async Task LobbyCreate_ValidData_Returns302ToDetail()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 36-06-PLAN.md")]
    public async Task LobbyCreate_InvalidMaxPlayers_Returns200WithError()
    {
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // MVCLOBBY-03: Join lobby — valid code joins, invalid/full show inline errors
    // -------------------------------------------------------------------------

    [Fact(Skip = "Wave 0 stub - implemented in 36-06-PLAN.md")]
    public async Task LobbyJoin_ValidInviteCode_Returns302ToDetail()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 36-06-PLAN.md")]
    public async Task LobbyJoin_InvalidCode_Returns200WithInlineError()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 36-06-PLAN.md")]
    public async Task LobbyJoin_FullLobby_Returns200WithInlineError()
    {
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // MVCLOBBY-04: Lobby detail — member sees player list and invite code
    // -------------------------------------------------------------------------

    [Fact(Skip = "Wave 0 stub - implemented in 36-06-PLAN.md")]
    public async Task LobbyDetail_AuthenticatedMember_Returns200WithPlayerListAndInviteCode()
    {
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // MVCLOBBY-05: Select faction — available succeeds, taken fails
    // -------------------------------------------------------------------------

    [Fact(Skip = "Wave 0 stub - implemented in 36-06-PLAN.md")]
    public async Task LobbySelectFaction_AvailableFaction_Returns302ToDetail()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 36-06-PLAN.md")]
    public async Task LobbySelectFaction_TakenFaction_Returns200WithError()
    {
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // MVCLOBBY-06: Leave lobby — member exits back to lobby home
    // -------------------------------------------------------------------------

    [Fact(Skip = "Wave 0 stub - implemented in 36-06-PLAN.md")]
    public async Task LobbyLeave_Member_Returns302ToLobbyHome()
    {
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // MVCLOBBY-07: Start game — host with 2+ players and factions succeeds, non-host fails
    // -------------------------------------------------------------------------

    [Fact(Skip = "Wave 0 stub - implemented in 36-06-PLAN.md")]
    public async Task LobbyStart_HostWith2PlayersAllFactions_Returns302()
    {
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 stub - implemented in 36-06-PLAN.md")]
    public async Task LobbyStart_NonHost_Returns200WithError()
    {
        await Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // MVCRT-01: Lobby detail page includes SignalR client script reference
    // -------------------------------------------------------------------------

    [Fact(Skip = "Wave 0 stub - implemented in 36-06-PLAN.md")]
    public async Task LobbyDetail_Html_ContainsSignalRScriptReference()
    {
        await Task.CompletedTask;
    }
}
