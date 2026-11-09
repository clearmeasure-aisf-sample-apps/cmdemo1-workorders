using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

[TestFixture]
public class StructureGuardrailGateTests
{
    private const string Playbook = "docs/refactoring-oversized-code.md";

    private static readonly Lazy<SourceSet> Repository = new(() => SourceSet.FromRepository(RepositoryCheckout.Current));

    private static IEnumerable<TestCaseData> Rules() =>
        new IStructureRule[]
            {
                new PartialTypeRule(),
                new StaticHelperClassRule(),
                new OwnTypeExtensionRule(),
                new BannedTypeNameRule(),
                new TestControlFlowRule(),
                new WholeScenarioTestRule(),
                new SetUpActAssertRule(),
                new TestNamespaceRule(),
                new TestSupportPlacementRule()
            }
            .Select(rule => new TestCaseData(rule).SetArgDisplayNames(rule.GetType().Name));

    [TestCaseSource(nameof(Rules))]
    public void ShouldAddNoViolation_WhenTheRepositoryIsScanned(IStructureRule rule)
    {
        var comparison = Compare(rule);

        comparison.Unexpected.ShouldBeEmpty(
            $"{rule.GetType().Name}: {rule.Guidance} Read and follow {Playbook} before editing. " +
            $"New violations:{Environment.NewLine}{string.Join(Environment.NewLine, comparison.Unexpected)}");
    }

    [TestCaseSource(nameof(Rules))]
    public void ShouldKeepTheBaselineTight_WhenAViolationIsFixed(IStructureRule rule)
    {
        var comparison = Compare(rule);

        comparison.Repeated.ShouldBeEmpty($"{BaselinePath(rule)} lists these keys more than once: {string.Join(", ", comparison.Repeated)}");
        comparison.Stale.ShouldBeEmpty(
            $"{rule.GetType().Name}: these lines are no longer violations; delete them from " +
            $"{BaselinePath(rule)} (baselines only shrink):{Environment.NewLine}" +
            string.Join(Environment.NewLine, comparison.Stale));
    }

    private static RatchetComparison Compare(IStructureRule rule) =>
        RepositoryCheckout.Current.ReadRatchet(BaselinePath(rule))
            .Compare(rule.FindViolations(Repository.Value).Select(violation => violation.Key));

    private static string BaselinePath(IStructureRule rule) =>
        $"src/UnitTests/BuildGates/Refactoring/Baselines/{rule.BaselineFileName}";
}
