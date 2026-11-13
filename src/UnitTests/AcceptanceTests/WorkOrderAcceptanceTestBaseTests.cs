using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceTests;

[TestFixture]
public class WorkOrderAcceptanceTestBaseTests
{
    public static IEnumerable<string> WorkOrdersFixturePaths()
    {
        var checkout = RepositoryCheckout.Current;
        return checkout.SourceFiles(".cs")
            .Select(checkout.Relative)
            .Where(path => path.StartsWith("src/AcceptanceTests/WorkOrders/", StringComparison.Ordinal));
    }

    public static IEnumerable<string> WorkOrdersParallelFixturePaths() =>
        WorkOrdersFixturePaths()
            .Where(path => !path.EndsWith("WorkOrderAcceptanceTestBase.cs", StringComparison.Ordinal))
            .Where(path => !path.EndsWith("WorkOrderSeedSpec.cs", StringComparison.Ordinal))
            .Where(path => !path.EndsWith("WorkOrderSaveThenAssignPersistenceTests.cs", StringComparison.Ordinal));

    [Test]
    public void WorkOrderAcceptanceTestBase_WhenRead_SeedsThroughObjectMother()
    {
        var source = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/WorkOrders/WorkOrderAcceptanceTestBase.cs");

        source.ShouldContain("class WorkOrderAcceptanceTestBase : AcceptanceTestBase");
        source.ShouldContain("SeedEmployeeAsync");
        source.ShouldContain("SeedWorkOrderAsync");
        source.ShouldContain("AcceptanceObjectMother");
        source.ShouldNotContain("NewDbContext");
        source.ShouldNotContain("SaveChangesAsync");
    }

    [TestCaseSource(nameof(WorkOrdersFixturePaths))]
    public void WorkOrdersFile_WhenRead_DoesNotUseDataContext(string path)
    {
        var text = RepositoryCheckout.Current.ReadText(path);
        text.ShouldNotContain("NewDbContext");
        text.ShouldNotContain("SaveChangesAsync");
    }

    [TestCaseSource(nameof(WorkOrdersParallelFixturePaths))]
    public void WorkOrdersFixture_WhenRead_UsesParallelScopeAll(string path)
    {
        RepositoryCheckout.Current.ReadText(path)
            .ShouldContain("Parallelizable(ParallelScope.All)");
    }

    [Test]
    public void SaveThenAssign_WhenRead_StaysOnSerialLane()
    {
        var text = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/WorkOrders/WorkOrderSaveThenAssignPersistenceTests.cs");
        text.ShouldContain("NonParallelizable");
        text.ShouldContain("Category(\"SerialLane\")");
    }
}
