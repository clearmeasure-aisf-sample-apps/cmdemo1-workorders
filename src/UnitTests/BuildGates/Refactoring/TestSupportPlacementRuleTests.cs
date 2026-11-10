using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

[TestFixture]
public class TestSupportPlacementRuleTests
{
    [TestCase("src/UnitTests/WorkOrders/Steps.cs", "public class WorkOrderSteps { }")]
    [TestCase("src/UnitTests/WorkOrders/WorkOrderSteps.cs", "public class WorkOrderSteps { public void Assign(WorkOrder o) => o.IsAssigned.ShouldBeTrue(); }")]
    [TestCase("src/UnitTests/WorkOrders/WorkOrderSteps.cs", "public class WorkOrderSteps { } public class AssignmentTests { [Test] public void ShouldAssign() { } }")]
    [TestCase("src/UnitTests/WorkOrders/WorkOrderSteps.cs", "public class AssignmentTests { private class WorkOrderSteps { } }")]
    public void ShouldFlagSteps_WhenTheyAreMisplacedOrAssert(string path, string code)
    {
        var sources = new SourceSetBuilder().WithFile(path, code).Build();

        var violations = SourceSetBuilder.KeysOf(new TestSupportPlacementRule(), sources);

        violations.ShouldBe([$"{path} :: WorkOrderSteps"]);
    }

    [TestCase("src/UnitTests/WorkOrders/WorkOrderSteps.cs", "public class WorkOrderSteps { public void Assign(WorkOrder o) => o.Assign(); }")]
    [TestCase("src/UnitTests/WorkOrders/WorkOrderBuilder.cs", "public class WorkOrderBuilder { public WorkOrder Build() => new(); }")]
    [TestCase("src/UnitTests/ObjectMother.cs", "public static class ObjectMother { }")]
    [TestCase("src/Core/Services/WorkOrderSteps.cs", "public class WorkOrderSteps { } public class X { private class Builder { } }")]
    public void ShouldPassTestSupport_WhenItHasItsOwnFileAndNoAssertions(string path, string code)
    {
        var sources = new SourceSetBuilder().WithFile(path, code).Build();

        var violations = SourceSetBuilder.KeysOf(new TestSupportPlacementRule(), sources);

        violations.ShouldBeEmpty();
    }
}
