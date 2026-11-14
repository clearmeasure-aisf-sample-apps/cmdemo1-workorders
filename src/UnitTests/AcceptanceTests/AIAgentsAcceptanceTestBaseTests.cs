using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceTests;

[TestFixture]
public class AIAgentsAcceptanceTestBaseTests
{
    [Test]
    public void AIAgentsAcceptanceTestBase_WhenRead_SeedsThroughApiBase()
    {
        var source = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/AIAgents/AIAgentsAcceptanceTestBase.cs");

        source.ShouldContain("class AIAgentsAcceptanceTestBase : AcceptanceApiTestBase");
        source.ShouldContain("OpenAiAgentPageAsync");
        source.ShouldContain("SnapshotAssigneeOrderIdsAsync");
        source.ShouldContain("LoadTaggedAssigneeOrdersAsync");
        source.ShouldNotContain("NewDbContext");
    }
}
