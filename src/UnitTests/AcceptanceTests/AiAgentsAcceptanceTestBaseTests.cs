using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceTests;

[TestFixture]
public class AiAgentsAcceptanceTestBaseTests
{
    [Test]
    public void AiAgentsAcceptanceTestBase_WhenRead_SeedsThroughApiBase()
    {
        var source = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/AIAgents/AiAgentsAcceptanceTestBase.cs");

        source.ShouldContain("class AiAgentsAcceptanceTestBase : AcceptanceApiTestBase");
        source.ShouldContain("OpenAiAgentPageAsync");
        source.ShouldContain("SnapshotAssigneeOrderIdsAsync");
        source.ShouldContain("LoadTaggedAssigneeOrdersAsync");
        source.ShouldNotContain("NewDbContext");
    }
}
