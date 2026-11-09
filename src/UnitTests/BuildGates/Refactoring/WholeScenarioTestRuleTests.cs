using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

[TestFixture]
public class WholeScenarioTestRuleTests
{
    [TestCase("[Test] public void ShouldAssign() { RunAssignmentScenario(); }")]
    [TestCase("[Test] public Task ShouldAssign() => RunAssignmentScenarioAsync();")]
    public void ShouldFlagTest_WhenOneHelperRunsTheWholeScenario(string test)
    {
        var sources = new SourceSetBuilder()
            .WithFile("src/UnitTests/Core/AssignmentTests.cs", $"public class AssignmentTests {{ {test} }}")
            .Build();

        var violations = SourceSetBuilder.KeysOf(new WholeScenarioTestRule(), sources);

        violations.ShouldBe(["src/UnitTests/Core/AssignmentTests.cs :: AssignmentTests.ShouldAssign"]);
    }

    [TestCase("[Test] public void ShouldAssign() { var order = Draft(); Assign(order); order.IsAssigned.ShouldBeTrue(); }")]
    [TestCase("[Test] public void ShouldLabel() => Label(High).ShouldBe(\"High\");")]
    [TestCase("[Test] public void ShouldReject() { Should.Throw<InvalidOperationException>(() => Assign(null)); }")]
    public void ShouldPassTest_WhenItsStepsAndAssertionAreVisible(string test)
    {
        var sources = new SourceSetBuilder()
            .WithFile("src/UnitTests/Core/AssignmentTests.cs", $"public class AssignmentTests {{ {test} }}")
            .Build();

        var violations = SourceSetBuilder.KeysOf(new WholeScenarioTestRule(), sources);

        violations.ShouldBeEmpty();
    }
}
