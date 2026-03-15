using System.Net;
using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration;

public class IntegrationTestHomeController : IntegrationTestBase
{
    public IntegrationTestHomeController(DatabaseFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Get_Index_RedirectsToAdmin()
    {
        var response = await Client.GetAsync("/");

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldBe("/root");
    }
}
