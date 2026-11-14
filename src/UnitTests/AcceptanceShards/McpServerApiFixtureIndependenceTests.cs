using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

/// <summary>Source-level checks that McpServer and Api fixtures stay API-seeded and ParallelScope.All.</summary>
[TestFixture]
public class McpServerApiFixtureIndependenceTests
{
    public static IEnumerable<string> McpServerAndApiFixturePaths()
    {
        var checkout = RepositoryCheckout.Current;
        return checkout.SourceFiles(".cs")
            .Select(checkout.Relative)
            .Where(path =>
                path.StartsWith("src/AcceptanceTests/McpServer/", StringComparison.Ordinal)
                || path.StartsWith("src/AcceptanceTests/Api/", StringComparison.Ordinal))
            .Where(path => path.EndsWith("Tests.cs", StringComparison.Ordinal));
    }

    [Test]
    public void McpAcceptanceTestBase_WhenRead_UsesApiSeedingAndParallelScopeAll()
    {
        var source = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/McpServer/McpAcceptanceTestBase.cs");
        source.ShouldContain("class McpAcceptanceTestBase : AcceptanceApiTestBase");
        source.ShouldContain("McpTestHelper");
        source.ShouldNotContain("NewDbContext");
        source.ShouldNotContain("OneTimeSetUp");
    }

    [Test]
    public void ApiAcceptanceTestBase_WhenRead_InheritsParallelApiBase()
    {
        var source = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/Api/ApiAcceptanceTestBase.cs");
        source.ShouldContain("class ApiAcceptanceTestBase : AcceptanceApiTestBase");
        source.ShouldContain("RequiresBrowser => false");
    }

    [TestCaseSource(nameof(McpServerAndApiFixturePaths))]
    public void McpOrApiFixture_WhenRead_DoesNotUseDataContext(string path)
    {
        var text = RepositoryCheckout.Current.ReadText(path);
        text.ShouldNotContain("NewDbContext");
        text.ShouldNotContain("SaveChangesAsync");
    }

    [TestCaseSource(nameof(McpServerAndApiFixturePaths))]
    public void McpOrApiFixture_WhenRead_IsParallelScopeAll(string path)
    {
        var text = RepositoryCheckout.Current.ReadText(path);
        var inheritsBase = text.Contains("McpAcceptanceTestBase", StringComparison.Ordinal)
            || text.Contains("ApiAcceptanceTestBase", StringComparison.Ordinal);
        var declaresAll = text.Contains("ParallelScope.All", StringComparison.Ordinal);
        (inheritsBase || declaresAll).ShouldBeTrue(path);
        text.ShouldNotContain("NonParallelizable");
        text.ShouldNotContain("SerialLane");
    }

    [Test]
    public void McpListWorkOrders_WhenRead_HasNoParityRetryLoop()
    {
        var text = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/McpServer/McpListWorkOrdersAcceptanceTests.cs");
        text.ShouldNotContain("attempt < 10");
        text.ShouldNotContain("siblings mutate");
        text.ShouldNotContain("Retry(2)");
        text.ShouldContain("AssertPortalAndMcpMatchAsync");
    }

    [Test]
    public void SerialLaneJson_WhenRead_OmitsMcpServerFixtures()
    {
        var text = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/acceptance-serial-lane.json");
        text.ShouldNotContain("McpListWorkOrdersAcceptanceTests");
        text.ShouldNotContain("McpWorkOrderLifecycleTests");
    }
}
