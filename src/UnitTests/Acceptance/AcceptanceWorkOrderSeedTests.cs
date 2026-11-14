using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Acceptance;

[TestFixture]
public class AcceptanceWorkOrderSeedTests
{
    [Test]
    public void AcceptanceWorkOrderSeed_WhenRead_ProbesThenFallsBackOn404()
    {
        var seed = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/Builders/AcceptanceWorkOrderSeed.cs");
        var sqlSeed = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/TestUsers/AcceptanceTestWorkOrders.cs");
        var mother = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/Builders/AcceptanceObjectMother.cs");
        var apiBase = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/Builders/AcceptanceApiTestBase.cs");
        var workOrderBase = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/WorkOrders/WorkOrderAcceptanceTestBase.cs");

        seed.ShouldContain("ProbeStatusAsync");
        seed.ShouldContain("HttpStatusCode.NotFound");
        seed.ShouldContain("AcceptanceTestWorkOrders.Add");
        seed.ShouldContain("CreateWorkOrderAsync");
        sqlSeed.ShouldContain("TestHost.NewDbContext");
        sqlSeed.ShouldContain("WorkOrderNumberGenerator");
        mother.ShouldContain("AcceptanceWorkOrderSeed.CreateAsync");
        apiBase.ShouldContain("Mother.WorkOrderAsync");
        workOrderBase.ShouldContain("Mother.WorkOrderAsync");
    }
}
