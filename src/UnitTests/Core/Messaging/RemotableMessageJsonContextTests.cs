using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Messaging;

[TestFixture]
public class RemotableMessageJsonContextTests
{
    [Test]
    public void Default_Should_KeepPascalCaseNames_So_RemotedBodiesMatchTheJitBus()
    {
        RemotableMessageJsonContext.Default.Options.PropertyNamingPolicy.ShouldBeNull();
    }

    [Test]
    public void RecentWorkOrdersQuery_Should_RoundTripItsCount()
    {
        var json = JsonSerializer.Serialize(new RecentWorkOrdersQuery(5), RemotableMessageJsonContext.Default.RecentWorkOrdersQuery);

        JsonSerializer.Deserialize(json, RemotableMessageJsonContext.Default.RecentWorkOrdersQuery)!.Count.ShouldBe(5);
    }
}
