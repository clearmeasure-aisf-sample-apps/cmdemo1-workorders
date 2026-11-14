using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Acceptance;

[TestFixture]
public class AcceptanceEmployeeSeedTests
{
    [Test]
    public void AcceptanceEmployeeSeed_WhenRead_ProbesThenFallsBackOn404()
    {
        var seed = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/Builders/AcceptanceEmployeeSeed.cs");
        var authBase = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/Authentication/AuthAcceptanceTestBase.cs");
        var apiBase = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/Builders/AcceptanceApiTestBase.cs");
        var workOrderBase = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/WorkOrders/WorkOrderAcceptanceTestBase.cs");

        seed.ShouldContain("ProbeStatusAsync");
        seed.ShouldContain("HttpStatusCode.NotFound");
        seed.ShouldContain("AcceptanceTestEmployees.Add");
        seed.ShouldContain("CreateEmployeeAsync");
        seed.ShouldContain("ToDirectoryDto");
        authBase.ShouldContain("AcceptanceEmployeeSeed.CreateAsync");
        authBase.ShouldContain("AcceptanceEmployeeSeed.ToDirectoryDto");
        apiBase.ShouldContain("AcceptanceEmployeeSeed.CreateAsync");
        workOrderBase.ShouldContain("AcceptanceEmployeeSeed.CreateAsync");
    }
}
