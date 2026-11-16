using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Contracts;

/// <summary>Pins the request body the API reads through its source-generated <see cref="ApiJsonContext"/> (Native AOT).</summary>
[TestFixture]
public class CaseRequestTests
{
    [Test]
    public void Deserialize_Should_ReadTextAndTargetCase_When_BodyUsesWebCamelCase()
    {
        var request = JsonSerializer.Deserialize("""{"text":"Hello World","to":"snake"}""", ApiJsonContext.Default.CaseRequest);

        request.ShouldBe(new CaseRequest("Hello World", "snake"));
    }
}
