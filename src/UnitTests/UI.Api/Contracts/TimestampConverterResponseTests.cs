using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Contracts;

/// <summary>Pins the response body the API writes through its source-generated <see cref="ApiJsonContext"/> (Native AOT).</summary>
[TestFixture]
public class TimestampConverterResponseTests
{
    [Test]
    public void Serialize_Should_WriteUnixIsoAndHumanInCamelCase()
    {
        var json = JsonSerializer.Serialize(new TimestampConverterResponse(0, "1970-01-01T00:00:00Z", "Thursday"), ApiJsonContext.Default.TimestampConverterResponse);

        json.ShouldBe("""{"unix":0,"iso":"1970-01-01T00:00:00Z","human":"Thursday"}""");
    }
}
