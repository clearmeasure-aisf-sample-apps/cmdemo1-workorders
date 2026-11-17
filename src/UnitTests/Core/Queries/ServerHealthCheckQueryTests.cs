using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

[TestFixture]
public class ServerHealthCheckQueryTests
{
    [Test]
    public void Query_Should_BeRemotable_And_SerializeAsAnEmptyBody()
    {
        var query = new ServerHealthCheckQuery();

        query.ShouldBeAssignableTo<IRemotableRequest>();
        JsonSerializer.Serialize(query, RemotableMessageJsonContext.Default.ServerHealthCheckQuery).ShouldBe("{}");
    }
}
