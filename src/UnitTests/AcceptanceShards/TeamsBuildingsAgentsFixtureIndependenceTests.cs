using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

/// <summary>
/// Source-level checks that TeamsAndVendors, Buildings, AIAgents, Extensions and NServiceBus
/// fixtures stay API-seeded and <see cref="NUnit.Framework.ParallelScope.All"/>.
/// </summary>
[TestFixture]
public class TeamsBuildingsAgentsFixtureIndependenceTests
{
    private static readonly string[] FixtureRoots =
    [
        "src/AcceptanceTests/TeamsAndVendors/",
        "src/AcceptanceTests/Buildings/",
        "src/AcceptanceTests/AIAgents/",
        "src/AcceptanceTests/Extensions/",
        "src/AcceptanceTests/NServiceBus/"
    ];

    public static IEnumerable<string> TargetFixturePaths()
    {
        var checkout = RepositoryCheckout.Current;
        return checkout.SourceFiles(".cs")
            .Select(checkout.Relative)
            .Where(path => FixtureRoots.Any(root => path.StartsWith(root, StringComparison.Ordinal)))
            .Where(path => path.EndsWith("Tests.cs", StringComparison.Ordinal));
    }

    [TestCase("src/AcceptanceTests/TeamsAndVendors/TeamsAndVendorsAcceptanceTestBase.cs")]
    [TestCase("src/AcceptanceTests/Buildings/BuildingsAcceptanceTestBase.cs")]
    [TestCase("src/AcceptanceTests/AIAgents/AIAgentsAcceptanceTestBase.cs")]
    public void DomainAcceptanceBase_WhenRead_ExtendsApiBaseWithoutDataContext(string path)
    {
        var source = RepositoryCheckout.Current.ReadText(path);
        source.ShouldContain("AcceptanceApiTestBase");
        source.ShouldNotContain("NewDbContext");
        source.ShouldNotContain("SaveChangesAsync");
    }

    [TestCaseSource(nameof(TargetFixturePaths))]
    public void TargetFixture_WhenRead_DoesNotUseDataContext(string path)
    {
        var text = RepositoryCheckout.Current.ReadText(path);
        text.ShouldNotContain("NewDbContext");
        text.ShouldNotContain("SaveChangesAsync");
    }

    [TestCaseSource(nameof(TargetFixturePaths))]
    public void TargetFixture_WhenRead_IsParallelScopeAll(string path)
    {
        var text = RepositoryCheckout.Current.ReadText(path);
        var inheritsApiBase = text.Contains("AcceptanceApiTestBase", StringComparison.Ordinal)
            || text.Contains("TeamsAndVendorsAcceptanceTestBase", StringComparison.Ordinal)
            || text.Contains("BuildingsAcceptanceTestBase", StringComparison.Ordinal)
            || text.Contains("AIAgentsAcceptanceTestBase", StringComparison.Ordinal);
        var declaresAll = text.Contains("ParallelScope.All", StringComparison.Ordinal);
        (inheritsApiBase || declaresAll).ShouldBeTrue(path);
        text.ShouldNotContain("NonParallelizable");
        text.ShouldNotContain("SerialLane");
    }
}
