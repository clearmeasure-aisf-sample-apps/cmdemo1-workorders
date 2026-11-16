using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Contracts;

/// <summary>Pins the response body the API writes through its source-generated <see cref="ApiJsonContext"/> (Native AOT).</summary>
[TestFixture]
public class HashResponseTests
{
    [Test]
    public void Serialize_Should_OmitMd5AndSha1_When_NotRequested()
    {
        var json = JsonSerializer.Serialize(new HashResponse("ba7816bf"), ApiJsonContext.Default.HashResponse);

        json.ShouldBe("""{"sha256":"ba7816bf"}""");
    }
}
