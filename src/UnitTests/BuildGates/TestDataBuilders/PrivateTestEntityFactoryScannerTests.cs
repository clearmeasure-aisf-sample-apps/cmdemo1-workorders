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
    public void ShouldFlagDefaultPrivateFactory_WhenAccessModifierIsOmitted()
    {
        const string code =
            """
            using ClearMeasure.Bootcamp.Core.Model;
            [TestFixture]
            public class SampleTests
            {
                [Test]
                public void ShouldArrange() { }
                static WorkOrder ImplicitPrivateOrder() => new WorkOrder();
            }
            """;

        var keys = PrivateTestEntityFactoryScanner.ScanFile("src/UnitTests/SampleTests.cs", code);

        keys.ShouldBe(["src/UnitTests/SampleTests.cs :: SampleTests.ImplicitPrivateOrder"]);
    }

    [Test]
    public void ShouldFlagPrivateFactory_ReturningWorkOrderArray()
    {
        const string code =
            """
            using ClearMeasure.Bootcamp.Core.Model;
            [TestFixture]
            public class SampleTests
            {
                [Test]
                public void ShouldArrange() { }
                private static WorkOrder[] SeedOrders() => [new WorkOrder()];
            }
            """;

        var keys = PrivateTestEntityFactoryScanner.ScanFile("src/UnitTests/SampleTests.cs", code);

        keys.ShouldBe(["src/UnitTests/SampleTests.cs :: SampleTests.SeedOrders"]);
    }

    [Test]
    public void ShouldIgnoreAsyncTaskFactories_AndBuilders()
    {
        const string code =
            """
            using ClearMeasure.Bootcamp.Core.Model;
            [TestFixture]
            public class SampleTests
            {
                private static async Task<WorkOrder[]> WhenSearch() => [];
                private static async Task<WorkOrder> SeedDraftAsync() => new WorkOrder();
            }
            public sealed class WorkOrderBuilder
            {
                private WorkOrder Seed() => new WorkOrder();
                public WorkOrder Build() => Seed();
            }
            """;

        var keys = PrivateTestEntityFactoryScanner.ScanFile("src/UnitTests/SampleTests.cs", code);

        keys.ShouldBeEmpty();
    }
}
