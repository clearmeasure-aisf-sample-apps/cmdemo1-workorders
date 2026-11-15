using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.TestDataBuilders;

[TestFixture]
public class PrivateTestEntityFactoryScannerTests
{
    [Test]
    public void ShouldFlagPrivateFactory_OnAFixture()
    {
        const string code =
            """
            using ClearMeasure.Bootcamp.Core.Model;
            [TestFixture]
            public class SampleTests
            {
                [Test]
                public void ShouldArrange() { }
                private static WorkOrder Order() => new WorkOrder();
                private static Employee NewEmployee() => new Employee("a", "b", "c", "d");
            }
            """;

        var keys = PrivateTestEntityFactoryScanner.ScanFile("src/UnitTests/SampleTests.cs", code);

        keys.ShouldBe([
            "src/UnitTests/SampleTests.cs :: SampleTests.Order",
            "src/UnitTests/SampleTests.cs :: SampleTests.NewEmployee"
        ]);
    }

    [Test]
    public void ShouldIgnorePrivateHelpers_OnBuilders()
    {
        const string code =
            """
            using ClearMeasure.Bootcamp.Core.Model;
            public sealed class WorkOrderBuilder
            {
                private WorkOrder Seed() => new WorkOrder();
                public WorkOrder Build() => Seed();
            }
            """;

        var keys = PrivateTestEntityFactoryScanner.ScanFile("src/UnitTests/TestSupport/WorkOrderBuilder.cs", code);

        keys.ShouldBeEmpty();
    }
}
