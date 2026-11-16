using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class PingControllerTests
{
    [Test]
    public void Get_Should_ReturnPlainTextPong()
    {
        var httpContext = new DefaultHttpContext();
        var result = PingEndpoint.Get(httpContext.Response);

        var content = result.ShouldBeContentResult();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        content.Content.ShouldBe("pong");
        httpContext.Response.Headers["X-Content-Type-Options"].ToString().ShouldBe("nosniff");
    }
}
