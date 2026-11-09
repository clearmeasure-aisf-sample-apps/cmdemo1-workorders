using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

[TestFixture]
public class SetUpActAssertRuleTests
{
    [TestCase("_calculator.Calculate(_order);")]
    [TestCase("new DueDateUrgencyCalculator().Calculate(_order);")]
    [TestCase("_order.DueDate.ShouldNotBeNull();")]
    [TestCase("Assert.That(_order, Is.Not.Null);")]
    public void ShouldFlagSetUp_WhenItActsOrAsserts(string statement)
    {
        var sources = new SourceSetBuilder()
            .WithFile("src/UnitTests/Core/DueDateUrgencyCalculatorTests.cs", Fixture("DueDateUrgencyCalculatorTests", statement))
            .Build();

        var violations = SourceSetBuilder.KeysOf(new SetUpActAssertRule(), sources);

        violations.ShouldBe(["src/UnitTests/Core/DueDateUrgencyCalculatorTests.cs :: DueDateUrgencyCalculatorTests.SetUp"]);
    }

    [Test]
    public void ShouldFlagSetUp_WhenItRendersTheComponentUnderTest()
    {
        var sources = new SourceSetBuilder()
            .WithFile("src/UnitTests/UI.Shared/WorkOrderSearchTests.cs", Fixture("WorkOrderSearchTests", "_page = _context.Render<WorkOrderSearch>();"))
            .Build();

        var violations = SourceSetBuilder.KeysOf(new SetUpActAssertRule(), sources);

        violations.ShouldBe(["src/UnitTests/UI.Shared/WorkOrderSearchTests.cs :: WorkOrderSearchTests.SetUp"]);
    }

    [TestCase("DueDateUrgencyCalculatorTests", "_order = ObjectMother.DraftWorkOrder(); Assert.Ignore(\"offline\");")]
    [TestCase("GivenAnAssignedWorkOrderTests", "_calculator.Calculate(_order);")]
    public void ShouldPassSetUp_WhenItArrangesOrTheFixtureNameSaysGiven(string fixture, string statement)
    {
        var sources = new SourceSetBuilder()
            .WithFile("src/UnitTests/Core/Sample.cs", Fixture(fixture, statement))
            .Build();

        var violations = SourceSetBuilder.KeysOf(new SetUpActAssertRule(), sources);

        violations.ShouldBeEmpty();
    }

    [Test]
    public void ShouldFlagGivenFixture_WhenItsSetUpAsserts()
    {
        var sources = new SourceSetBuilder()
            .WithFile("src/UnitTests/Core/Sample.cs", Fixture("GivenAnAssignedWorkOrderTests", "_order.ShouldNotBeNull();"))
            .Build();

        var violations = SourceSetBuilder.KeysOf(new SetUpActAssertRule(), sources);

        violations.ShouldBe(["src/UnitTests/Core/Sample.cs :: GivenAnAssignedWorkOrderTests.SetUp"]);
    }

    private static string Fixture(string name, string setUpStatement) =>
        $$"""
          public class {{name}}
          {
              private DueDateUrgencyCalculator _calculator = new();
              private WorkOrder _order = null!;

              [SetUp]
              public void SetUp() { {{setUpStatement}} }
          }
          """;
}
