using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EchoEndpointTests
{
    [Test]
    public void Get_Should_ReflectMethodPathQueryAndHeaders()
    {
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Method = "GET",
                Scheme = "https",
                Host = new HostString("localhost:7174"),
                Path = "/api/echo",
                QueryString = new QueryString("?foo=bar&x=1"),
                Protocol = "HTTP/2"
            },
            Connection =
            {
                RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1")
            }
        };
        httpContext.Request.Headers["X-Debug"] = "trace-1";

        var result = EchoEndpoint.Get(httpContext);

        var ok = result.ShouldBeOkResult();
        var payload = ok.Value.ShouldBeOfType<EchoResponse>();
        payload.Method.ShouldBe("GET");
        payload.Path.ShouldBe("/api/echo");
        payload.QueryString.ShouldBe("?foo=bar&x=1");
        payload.Scheme.ShouldBe("https");
        payload.Host.ShouldBe("localhost:7174");
        payload.Protocol.ShouldBe("HTTP/2");
        payload.RemoteIpAddress.ShouldBe("127.0.0.1");
        payload.Headers["X-Debug"].ShouldBe("trace-1");
    }
}
