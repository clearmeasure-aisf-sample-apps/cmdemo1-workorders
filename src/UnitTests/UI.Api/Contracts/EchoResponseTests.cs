using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Contracts;

/// <summary>Pins the echo body: written through the source-generated <see cref="ApiJsonContext"/> and read back by clients.</summary>
[TestFixture]
public class EchoResponseTests
{
    [Test]
    public void Serialize_Should_RoundTripEveryField_When_WrittenAndReadWithApiJsonContext()
    {
        var echo = new EchoResponse
        {
            Method = "GET",
            Path = "/api/echo",
            QueryString = "?x=1",
            Scheme = "https",
            Host = "localhost",
            Protocol = "HTTP/2",
            RemoteIpAddress = null,
            Headers = new Dictionary<string, string> { ["X-Debug"] = "trace-1" }
        };

        var json = JsonSerializer.Serialize(echo, ApiJsonContext.Default.EchoResponse);
        var read = JsonSerializer.Deserialize(json, ApiJsonContext.Default.EchoResponse)!;

        json.ShouldStartWith("""{"method":"GET","path":"/api/echo","queryString":"?x=1",""");
        read.Headers["X-Debug"].ShouldBe("trace-1");
        read.RemoteIpAddress.ShouldBeNull();
    }
}
