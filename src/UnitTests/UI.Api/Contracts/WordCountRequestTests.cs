using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Contracts;

/// <summary>Pins the request body the API reads through its source-generated <see cref="ApiJsonContext"/> (Native AOT).</summary>
[TestFixture]
public class WordCountRequestTests
{
    [Test]
    public void Deserialize_Should_ReadText_When_BodyUsesWebCamelCase()
    {
        var request = JsonSerializer.Deserialize("""{"text":"one two"}""", ApiJsonContext.Default.WordCountRequest);

        request.ShouldBe(new WordCountRequest("one two"));
    }
}
