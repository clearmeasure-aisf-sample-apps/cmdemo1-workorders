using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Deterministic gate for #1805: field rules and messages live in Core Model/Validation only.
/// </summary>
[TestFixture]
public class WorkOrderFieldMessageArchitectureTests
{
    [Test]
    public void ShouldKeepFieldMessagesInCore_WhenProductionSourcesAreScanned()
    {
        var violations = Scan(RepositoryCheckout.Current);

        violations.ShouldBeEmpty(
            "Work-order field messages outside Core/Model and Core/Validation: "
            + string.Join(", ", violations));
    }

    [Test]
    public void ShouldFlagCannotExceedLiteral_WhenOutsideCoreModelOrValidation()
    {
        var violations = WorkOrderFieldMessageRules.Violations(
            "src/UI.Shared/Models/Sample.cs",
            """
            namespace Sample;
            public class Sample
            {
                public const string Message = "Title cannot exceed 600 characters.";
            }
            """);

        violations.ShouldContain(v => v.Contains("cannot exceed", StringComparison.Ordinal));
    }

    [Test]
    public void ShouldFlagHardCodedStringLengthErrorMessage_WhenOutsideCore()
    {
        var violations = WorkOrderFieldMessageRules.Violations(
            "src/UI.Shared/Models/Sample.cs",
            """
            using System.ComponentModel.DataAnnotations;
            namespace Sample;
            public class Sample
            {
                [StringLength(10, ErrorMessage = "Title cannot exceed 10 characters.")]
                public string? Title { get; set; }
            }
            """);

        violations.ShouldContain(v => v.Contains("hard-coded ErrorMessage", StringComparison.Ordinal));
    }

    [Test]
    public void ShouldAllowCoreModelLiterals_WhenInWorkOrderFieldRules()
    {
        var violations = WorkOrderFieldMessageRules.Violations(
            "src/Core/Model/WorkOrderFieldRules.cs",
            """
            namespace ClearMeasure.Bootcamp.Core.Model;
            public static class WorkOrderFieldRules
            {
                public const string TitleCannotExceed = "Title cannot exceed 600 characters.";
            }
            """);

        violations.ShouldBeEmpty();
    }

    private static IReadOnlyList<string> Scan(RepositoryCheckout checkout)
    {
        var violations = new List<string>();
        foreach (var file in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(file);
            violations.AddRange(WorkOrderFieldMessageRules.Violations(relative, File.ReadAllText(file)));
        }

        return violations.Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToList();
    }
}
