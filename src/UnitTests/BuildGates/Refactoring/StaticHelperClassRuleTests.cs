using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

[TestFixture]
public class StaticHelperClassRuleTests
{
    [TestCase("WorkOrderHelper")]
    [TestCase("WorkOrderHelpers")]
    [TestCase("StringUtil")]
    [TestCase("StringUtils")]
    [TestCase("WorkOrderStringUtility")]
    [TestCase("DateUtilities")]
    public void ShouldFlagStaticClass_WhenItIsNamedLikeAHelper(string name)
    {
        var sources = new SourceSetBuilder().WithFile("src/Core/Sample.cs", $"public static class {name} {{ }}").Build();

        var violations = SourceSetBuilder.KeysOf(new StaticHelperClassRule(), sources);

        violations.ShouldBe([$"src/Core/Sample.cs :: {name}"]);
    }

    [TestCase("public static class DueDateUrgencyCalculator { }")]
    [TestCase("public class SpeechHelper { }")]
    public void ShouldPass_WhenTheClassNamesAConceptOrIsNotStatic(string code)
    {
        var sources = new SourceSetBuilder().WithFile("src/Core/Sample.cs", code).Build();

        var violations = SourceSetBuilder.KeysOf(new StaticHelperClassRule(), sources);

        violations.ShouldBeEmpty();
    }
}
