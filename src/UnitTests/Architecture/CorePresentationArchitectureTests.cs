using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Deterministic gate for #1812: Core may not hand out CSS class names or *Css* members.
/// </summary>
[TestFixture]
public class CorePresentationArchitectureTests
{
    [Test]
    public void ShouldRejectCssLikeLiteral_WhenSampleCoreSourceContainsBtnClass()
    {
        var violations = CorePresentationRules.Violations(
            "src/Core/Services/Sample.cs",
            """
            namespace Sample;
            public static class Sample
            {
                public static string Style() => "btn-primary";
            }
            """);

        violations.ShouldContain(v => v.Contains("btn-primary", StringComparison.Ordinal));
    }

    [Test]
    public void ShouldRejectCssNamedMember_WhenSampleCoreSourceDeclaresCssClass()
    {
        var violations = CorePresentationRules.Violations(
            "src/Core/Services/Sample.cs",
            """
            namespace Sample;
            public static class Sample
            {
                public static string CssClass() => "x";
            }
            """);

        violations.ShouldContain(v => v.Contains("member:CssClass", StringComparison.Ordinal));
    }

    [Test]
    public void ShouldAllowNonCssCoreSource_WhenNoPresentationLeak()
    {
        var violations = CorePresentationRules.Violations(
            "src/Core/Services/Sample.cs",
            """
            namespace Sample;
            public static class Sample
            {
                public static string Label() => "Overdue";
            }
            """);

        violations.ShouldBeEmpty();
    }

    [Test]
    public void ShouldIgnoreUiShared_WhenCssLivesOutsideCore()
    {
        var violations = CorePresentationRules.Violations(
            "src/UI.Shared/Models/Sample.cs",
            """
            namespace Sample;
            public static class Sample
            {
                public static string CssClass() => "btn-primary";
            }
            """);

        violations.ShouldBeEmpty();
    }

    [Test]
    public void ShouldKeepCoreFreeOfCss_WhenMasterSourcesAreScanned()
    {
        var violations = Scan(RepositoryCheckout.Current);

        violations.ShouldBeEmpty(
            "Core must not contain CSS-like string literals or *Css* members. Found: "
            + string.Join(", ", violations));
    }

    private static IReadOnlyList<string> Scan(RepositoryCheckout checkout)
    {
        var violations = new List<string>();
        foreach (var sourceFile in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(sourceFile);
            violations.AddRange(CorePresentationRules.Violations(relative, File.ReadAllText(sourceFile)));
        }

        return violations;
    }
}
