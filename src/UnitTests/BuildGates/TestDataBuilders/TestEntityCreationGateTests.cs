using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.TestDataBuilders;

/// <summary>
/// Ratchets <c>new WorkOrder</c> / <c>new Employee(</c> outside builders and mothers, and private
/// fixture methods that return those types. Baselines only shrink.
/// </summary>
[TestFixture]
public class TestEntityCreationGateTests
{
    private const string CreationBaseline =
        "src/UnitTests/BuildGates/TestDataBuilders/test-entity-creation-baseline.txt";

    private const string FactoryBaseline =
        "src/UnitTests/BuildGates/TestDataBuilders/private-test-entity-factory-baseline.txt";

    [Test]
    public void ShouldAddNoCreation_OutsideBuildersAndMothers()
    {
        var comparison = CompareCreations();

        comparison.Unexpected.ShouldBeEmpty(comparison.Report(
            "Test entity creation",
            "Move the creation into WorkOrderBuilder, EmployeeBuilder or ObjectMother; do not add baseline lines."));
    }

    [Test]
    public void ShouldKeepCreationBaselineTight_WhenCreationsMoveIntoBuilders()
    {
        var comparison = CompareCreations();

        comparison.Repeated.ShouldBeEmpty($"{CreationBaseline} lists these keys more than once: {string.Join(", ", comparison.Repeated)}");
        comparison.Stale.ShouldBeEmpty(
            $"These creation keys are gone; delete them from {CreationBaseline}:{Environment.NewLine}" +
            string.Join(Environment.NewLine, comparison.Stale));
    }

    [Test]
    public void ShouldAddNoPrivateFixtureFactory_ReturningWorkOrderOrEmployee()
    {
        var comparison = CompareFactories();

        comparison.Unexpected.ShouldBeEmpty(comparison.Report(
            "Private test entity factory",
            "Replace the private factory with WorkOrderBuilder, EmployeeBuilder or ObjectMother; do not add baseline lines."));
    }

    [Test]
    public void ShouldKeepFactoryBaselineTight_WhenPrivateFactoriesAreRemoved()
    {
        var comparison = CompareFactories();

        comparison.Repeated.ShouldBeEmpty($"{FactoryBaseline} lists these keys more than once: {string.Join(", ", comparison.Repeated)}");
        comparison.Stale.ShouldBeEmpty(
            $"These private factories are gone; delete them from {FactoryBaseline}:{Environment.NewLine}" +
            string.Join(Environment.NewLine, comparison.Stale));
    }

    private static RatchetComparison CompareCreations() =>
        RepositoryCheckout.Current.ReadRatchet(CreationBaseline)
            .Compare(TestEntityCreationScanner.ScanRepository(RepositoryCheckout.Current));

    private static RatchetComparison CompareFactories() =>
        RepositoryCheckout.Current.ReadRatchet(FactoryBaseline)
            .Compare(PrivateTestEntityFactoryScanner.ScanRepository(RepositoryCheckout.Current));
}
