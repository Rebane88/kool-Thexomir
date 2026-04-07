using RealmsOfAsh.Tests.Fixtures;
using Xunit;

namespace RealmsOfAsh.Tests.Integration.Api;

public class PublicGameTurnTimerTests : IntegrationTestBase
{
    public PublicGameTurnTimerTests(DatabaseFixture fixture) : base(fixture) { }

    [Fact(Skip = "Wave 0 placeholder — implemented in 38-01")]
    public async Task MVCGAME_15_TurnTimer_RendersWhenDeadlineSet()
    {
        // Wave 1 will: seed an active game, GET /Public/Game/{id}, assert HTML contains
        //   id="turn-timer" AND a <script> block referencing setInterval and the deadline.
        await Task.CompletedTask;
    }

    [Fact(Skip = "Wave 0 placeholder — implemented in 38-01")]
    public async Task MVCGAME_15_TurnTimer_HiddenWhenNoDeadline()
    {
        // Wave 1 will: seed a game with TurnDeadline = null, GET the page, assert no
        //   setInterval-based timer script is emitted (the span may exist but the script must not).
        await Task.CompletedTask;
    }
}
