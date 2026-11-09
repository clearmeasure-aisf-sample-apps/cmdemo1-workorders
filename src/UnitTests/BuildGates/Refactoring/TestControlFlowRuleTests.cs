using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

[TestFixture]
public class TestControlFlowRuleTests
{
    [TestCase("if (order.IsOverdue()) { order.Escalate(); }")]
    [TestCase("foreach (var order in orders) { order.Assign(); }")]
    [TestCase("for (var i = 0; i < 3; i++) { Assign(); }")]
    [TestCase("while (Pending()) { Assign(); }")]
    [TestCase("var label = overdue ? \"late\" : \"ok\";")]
    [TestCase("var label = status switch { _ => \"ok\" };")]
    public void ShouldFlagTestMethod_WhenItLoopsOrBranches(string statement)
    {
        var sources = new SourceSetBuilder()
            .WithFile("src/UnitTests/Core/AssignmentTests.cs", TestFixtureWith($"[Test] public void ShouldAssign() {{ {statement} }}"))
            .Build();

        var violations = SourceSetBuilder.KeysOf(new TestControlFlowRule(), sources);

        violations.ShouldBe(["src/UnitTests/Core/AssignmentTests.cs :: AssignmentTests.ShouldAssign"]);
    }

    [Test]
    public void ShouldPassTestMethod_WhenItReadsAsAListOfSteps()
    {
        var sources = new SourceSetBuilder()
            .WithFile(
                "src/UnitTests/Core/AssignmentTests.cs",
                TestFixtureWith(
                    """
                    [TestCase(1)]
                    public void ShouldAssign(int count)
                    {
                        var order = ObjectMother.DraftWorkOrder();
                        WorkOrderSteps.Assign(order);
                        order.Status.ShouldBe(WorkOrderStatus.Assigned);
                    }
                    private void Helper() { if (true) { } }
                    """))
            .Build();

        var violations = SourceSetBuilder.KeysOf(new TestControlFlowRule(), sources);

        violations.ShouldBeEmpty();
    }

    [Test]
    public void ShouldIgnoreProductionCode_WhenItBranches()
    {
        var sources = new SourceSetBuilder()
            .WithFile("src/Core/Assignment.cs", TestFixtureWith("[Test] public void ShouldAssign() { if (true) { } }"))
            .Build();

        var violations = SourceSetBuilder.KeysOf(new TestControlFlowRule(), sources);

        violations.ShouldBeEmpty();
    }

    private static string TestFixtureWith(string members) => $"public class AssignmentTests {{ {members} }}";
}
