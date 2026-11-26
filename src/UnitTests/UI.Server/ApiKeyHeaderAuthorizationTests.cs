using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiKeyHeaderAuthorizationTests
{
    [Test]
    public void IsAuthorized_ReturnsTrue_WhenKeyMatches()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers[ApiKeyConstants.HeaderName] = "secret";
        ApiKeyHeaderAuthorization.IsAuthorized(request, "secret").ShouldBeTrue();
    }

    [Test]
    public void IsAuthorized_ReturnsFalse_WhenKeyMissing()
    {
        var request = new DefaultHttpContext().Request;
        ApiKeyHeaderAuthorization.IsAuthorized(request, "secret").ShouldBeFalse();
    }
}
