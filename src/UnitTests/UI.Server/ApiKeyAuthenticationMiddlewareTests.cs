using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiKeyAuthenticationMiddlewareTests
{
    [Test]
    public void IsAuthorized_ReturnsTrue_WhenKeyMatches()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers["X-API-Key"] = "secret";
        ApiKeyAuthenticationMiddleware.IsAuthorized(request, "secret").ShouldBeTrue();
    }

    [Test]
    public void IsAuthorized_ReturnsFalse_WhenKeyMissing()
    {
        var request = new DefaultHttpContext().Request;
        ApiKeyAuthenticationMiddleware.IsAuthorized(request, "secret").ShouldBeFalse();
    }
}
