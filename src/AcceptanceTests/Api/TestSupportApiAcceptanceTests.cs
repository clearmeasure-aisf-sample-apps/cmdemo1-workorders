using System.Net;
using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

/// <summary>
/// Proves test-support is on for the local acceptance host (pool-slot shape) and 404 on non-pool remotes.
/// </summary>
[TestFixture]
public class TestSupportApiAcceptanceTests : ApiAcceptanceTestBase
{
    [Test]
    public async Task Should_CreateEmployee_When_LocalHostEnablesTestSupport()
    {
        LocalAcceptanceHost.Require();

        var tag = Guid.NewGuid().ToString("N")[..8];
        var mother = new AcceptanceObjectMother(AcceptanceTestSupportSettings.CreateClient(), tag);
        var employee = await mother.EmployeeAsync();
        var workOrder = await mother.DraftWorkOrderAsync(employee.UserName);

        employee.UserName.ShouldStartWith($"test_{tag}_");
        workOrder.CreatorUserName.ShouldBe(employee.UserName);
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
