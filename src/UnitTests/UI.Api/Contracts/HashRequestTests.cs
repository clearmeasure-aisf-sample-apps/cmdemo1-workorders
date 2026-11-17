using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Contracts;

/// <summary>Pins the request body the API reads through its source-generated <see cref="ApiJsonContext"/> (Native AOT).</summary>
[TestFixture]
public class HashRequestTests
{
    [Test]
    public void Deserialize_Should_DefaultSha1Off_When_OnlyMd5IsRequested()
    {
        var request = JsonSerializer.Deserialize("""{"text":"abc","includeMd5":true}""", ApiJsonContext.Default.HashRequest);

        request.ShouldBe(new HashRequest("abc", IncludeMd5: true));
    }
}
