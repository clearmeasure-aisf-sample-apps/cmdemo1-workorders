using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Contracts;

/// <summary>Pins the response body the API writes through its source-generated <see cref="ApiJsonContext"/> (Native AOT).</summary>
[TestFixture]
public class UppercaseResponseTests
{
    [Test]
    public void Serialize_Should_WriteTextInCamelCase()
    {
        var json = JsonSerializer.Serialize(new UppercaseResponse("HELLO"), ApiJsonContext.Default.UppercaseResponse);

        json.ShouldBe("""{"text":"HELLO"}""");
    }
}
