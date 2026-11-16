using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ServerJsonContextTests
{
    [Test]
    public void Options_Should_UseWebDefaults_So_ServerBodiesAreCamelCase()
    {
        ServerJsonContext.Default.Options.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
    }
}
