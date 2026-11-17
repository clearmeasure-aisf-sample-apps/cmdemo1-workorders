using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Contracts;

/// <summary>Pins the request body the API reads through its source-generated <see cref="ApiJsonContext"/> (Native AOT).</summary>
[TestFixture]
public class Base64RequestTests
{
    [Test]
    public void Deserialize_Should_ReadTextAndMode_When_BodyUsesWebCamelCase()
    {
        var request = JsonSerializer.Deserialize("""{"text":"aGVsbG8=","mode":"decode"}""", ApiJsonContext.Default.Base64Request);

        request.ShouldBe(new Base64Request("aGVsbG8=", "decode"));
    }
}
