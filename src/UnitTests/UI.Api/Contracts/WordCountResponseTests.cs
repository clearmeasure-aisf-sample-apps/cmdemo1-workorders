using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Contracts;

/// <summary>Pins the response body the API writes through its source-generated <see cref="ApiJsonContext"/> (Native AOT).</summary>
[TestFixture]
public class WordCountResponseTests
{
    [Test]
    public void Serialize_Should_WriteCountsInCamelCase()
    {
        var json = JsonSerializer.Serialize(new WordCountResponse(2, 7, 1), ApiJsonContext.Default.WordCountResponse);

        json.ShouldBe("""{"words":2,"characters":7,"lines":1}""");
    }
}
