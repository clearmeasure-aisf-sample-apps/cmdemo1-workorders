using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

[TestFixture]
public class TestNamespaceRuleTests
{
    [TestCase("namespace ClearMeasure.Bootcamp.UnitTests.WorkOrders; public class AssignmentTests { }", "ClearMeasure.Bootcamp.UnitTests.WorkOrders")]
    [TestCase("public class AssignmentTests { }", "(global)")]
    public void ShouldFlagTestFile_WhenItsNamespaceDoesNotMatchItsFolder(string code, string declared)
    {
        var sources = new SourceSetBuilder()
            .WithFile("src/UnitTests/WorkOrders/Assignment/AssignmentTests.cs", code)
            .Build();

        var violations = SourceSetBuilder.KeysOf(new TestNamespaceRule(), sources);

        violations.ShouldBe([$"src/UnitTests/WorkOrders/Assignment/AssignmentTests.cs :: {declared}"]);
    }

    [TestCase("src/UnitTests/WorkOrders/Assignment/AssignmentTests.cs", "ClearMeasure.Bootcamp.UnitTests.WorkOrders.Assignment")]
    [TestCase("src/AcceptanceTests/UI.Shared/LoginTests.cs", "ClearMeasure.Bootcamp.AcceptanceTests.UI.Shared")]
    public void ShouldPassTestFile_WhenItsNamespaceMatchesItsFolder(string path, string declared)
    {
        var sources = new SourceSetBuilder()
            .WithFile(path, $"namespace {declared}; public class Sample {{ }}")
            .Build();

        var violations = SourceSetBuilder.KeysOf(new TestNamespaceRule(), sources);

        violations.ShouldBeEmpty();
    }
}
