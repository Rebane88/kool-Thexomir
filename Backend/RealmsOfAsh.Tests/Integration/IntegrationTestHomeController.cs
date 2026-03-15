using RealmsOfAsh.Tests.Fixtures;
using Shouldly;

namespace RealmsOfAsh.Tests.Integration;

public class IntegrationTestHomeController : IntegrationTestBase
{
    public IntegrationTestHomeController(DatabaseFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Get_Index_IsSuccessful()
    {
        var response = await Client.GetAsync("/");

        response.IsSuccessStatusCode.ShouldBeTrue();
    }
}
