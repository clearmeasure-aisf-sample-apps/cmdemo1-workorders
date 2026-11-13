using System.Net;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

/// <summary>
/// Proves test-support is on for the local acceptance host (pool-slot shape) and 404 on non-pool remotes.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public class TestSupportApiAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task Should_CreateEmployee_When_LocalHostEnablesTestSupport()
    {
        LocalAcceptanceHost.Require();

        var client = AcceptanceTestSupportSettings.CreateClient();
        var tag = Guid.NewGuid().ToString("N")[..8];
        var employee = await client.CreateEmployeeAsync(tag);

        employee.UserName.ShouldStartWith($"test_{tag}_");
    }

    [Test]
    public async Task Should_Return404_When_RemoteHostIsNotAPoolSlot()
    {
        LocalAcceptanceHost.RequireRemoteNonPool();

        var client = AcceptanceTestSupportSettings.CreateClient();
        var status = await client.ProbeStatusAsync();
        status.ShouldBe(HttpStatusCode.NotFound);
    }
}
