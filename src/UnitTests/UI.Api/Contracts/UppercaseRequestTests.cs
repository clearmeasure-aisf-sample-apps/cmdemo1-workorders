using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Contracts;

/// <summary>Pins the request body the API reads through its source-generated <see cref="ApiJsonContext"/> (Native AOT).</summary>
[TestFixture]
public class UppercaseRequestTests
{
    [Test]
    public void Deserialize_Should_LeaveTextNull_When_BodyOmitsIt()
    {
        var request = JsonSerializer.Deserialize("""{}""", ApiJsonContext.Default.UppercaseRequest);

        request.ShouldBe(new UppercaseRequest(null));
    }
}
