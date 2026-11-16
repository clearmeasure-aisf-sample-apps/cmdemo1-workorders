using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Contracts;

/// <summary>Pins the response body the API writes through its source-generated <see cref="ApiJsonContext"/> (Native AOT).</summary>
[TestFixture]
public class SlugifyResponseTests
{
    [Test]
    public void Serialize_Should_WriteSlugInCamelCase()
    {
        var json = JsonSerializer.Serialize(new SlugifyResponse("hello-world"), ApiJsonContext.Default.SlugifyResponse);

        json.ShouldBe("""{"slug":"hello-world"}""");
    }
}
