using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>Pins the server version body written through the source-generated <see cref="ServerJsonContext"/>.</summary>
[TestFixture]
public class ServerVersionTests
{
    [TestCase("2.4.1", """{"version":"2.4.1"}""")]
    [TestCase(null, """{"version":null}""")]
    public void Serialize_Should_WriteTheVersionInCamelCase(string? version, string expected)
    {
        JsonSerializer.Serialize(new ServerVersion(version), ServerJsonContext.Default.ServerVersion).ShouldBe(expected);
    }
}
