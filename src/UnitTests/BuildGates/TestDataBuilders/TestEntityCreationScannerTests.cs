using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.TestDataBuilders;

[TestFixture]
public class TestEntityCreationScannerTests
{
    [Test]
    public void ShouldCountCreations_OutsideBuilders()
    {
        const string code =
            """
            using ClearMeasure.Bootcamp.Core.Model;
            public class SampleTests
            {
                [Test]
                public void ShouldArrange()
                {
                    var employee = new Employee("a", "b", "c", "d");
                    var order = new WorkOrder { Creator = employee };
                }
            }
            """;

        var keys = TestEntityCreationScanner.ScanFile("src/UnitTests/SampleTests.cs", code);

        keys.ShouldBe([
            "Employee src/UnitTests/SampleTests.cs#1",
            "WorkOrder src/UnitTests/SampleTests.cs#1"
        ]);
    }

    [Test]
    public void ShouldCountTargetTypedCreations_WhenDeclaredTypeIsWorkOrderOrEmployee()
    {
        const string code =
            """
            using ClearMeasure.Bootcamp.Core.Model;
            public class SampleTests
            {
                [Test]
                public void ShouldArrange()
                {
                    WorkOrder order = new() { Title = "t" };
                    Employee employee = new("a", "b", "c", "d");
                }
                private WorkOrder Seed() => new() { Title = "seed" };
            }
            """;

        var keys = TestEntityCreationScanner.ScanFile("src/UnitTests/SampleTests.cs", code);

        keys.ShouldBe([
            "Employee src/UnitTests/SampleTests.cs#1",
            "WorkOrder src/UnitTests/SampleTests.cs#1",
            "WorkOrder src/UnitTests/SampleTests.cs#2"
        ]);
    }

    [Test]
    public void ShouldIgnoreCreations_InsideBuilderTypes()
    {
        const string code =
            """
            using ClearMeasure.Bootcamp.Core.Model;
            public sealed class WorkOrderBuilder
            {
                public WorkOrder Build() => new WorkOrder();
            }
            public sealed class EmployeeBuilder
            {
                public Employee Build() => new Employee("a", "b", "c", "d");
            }
            """;

        var keys = TestEntityCreationScanner.ScanFile("src/UnitTests/TestSupport/WorkOrderBuilder.cs", code);

        keys.ShouldBeEmpty();
    }
}
