using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

[TestFixture]
public class StructureGuardrailGateTests
{
    private const string Playbook = "docs/refactoring-oversized-code.md";

    private static readonly Lazy<SourceSet> Repository = new(() => SourceSet.FromRepository(FindRepoRoot()));

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

        comparison.NewViolations.ShouldBeEmpty(
            $"{rule.GetType().Name}: {rule.Guidance} Read and follow {Playbook} before editing. " +
            $"New violations:{Environment.NewLine}{string.Join(Environment.NewLine, comparison.NewViolations)}");
    }

    [TestCaseSource(nameof(Rules))]
    public void ShouldKeepTheBaselineTight_WhenAViolationIsFixed(IStructureRule rule)
    {
        var comparison = Compare(rule);

        comparison.StaleEntries.ShouldBeEmpty(
            $"{rule.GetType().Name}: these lines are no longer violations; delete them from " +
            $"{BaselinePath(rule)} (baselines only shrink):{Environment.NewLine}" +
            string.Join(Environment.NewLine, comparison.StaleEntries));
    }

    private static RatchetComparison Compare(IStructureRule rule) =>
        RatchetBaseline.Load(BaselinePath(rule)).Compare(rule.FindViolations(Repository.Value));

    private static string BaselinePath(IStructureRule rule) =>
        Path.Combine(FindRepoRoot(), "src", "UnitTests", "BuildGates", "Refactoring", "Baselines", rule.BaselineFileName);

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "ChurchBulletin.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root not found from test directory.");
    }
}
