using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EchoControllerHeaderRedactionTests
{
    [Test]
    public void Get_Should_RedactAuthorizationApiKeyAndCookie()
    {
        var httpContext = EchoEndpointRequestSteps.CreateGetContext();
        EchoEndpointRequestSteps.SetHeader(httpContext, "Authorization", "Bearer secret-token");
        EchoEndpointRequestSteps.SetHeader(httpContext, "X-Api-Key", "api-key-value");
        EchoEndpointRequestSteps.SetHeader(httpContext, "Cookie", "session=abc");
        EchoEndpointRequestSteps.SetHeader(httpContext, "Accept", "application/json");

        var result = EchoEndpointRequestSteps.InvokeGet(httpContext);

        var payload = result.ShouldBeOkResult().Value.ShouldBeOfType<EchoResponse>();
        payload.Headers["Authorization"].ShouldBe(EchoEndpoint.RedactedValue);
        payload.Headers["X-Api-Key"].ShouldBe(EchoEndpoint.RedactedValue);
        payload.Headers["Cookie"].ShouldBe(EchoEndpoint.RedactedValue);
        payload.Headers["Accept"].ShouldBe("application/json");
    }

    [Test]
    public void Get_Should_RedactSetCookieHeader()
    {
        var httpContext = EchoEndpointRequestSteps.CreateGetContext();
        EchoEndpointRequestSteps.SetHeader(httpContext, "Set-Cookie", "auth=super-secret; Path=/; HttpOnly");
        EchoEndpointRequestSteps.SetHeader(httpContext, "User-Agent", "EchoEndpointTests");

        var result = EchoEndpointRequestSteps.InvokeGet(httpContext);

        var payload = result.ShouldBeOkResult().Value.ShouldBeOfType<EchoResponse>();
        payload.Headers["Set-Cookie"].ShouldBe(EchoEndpoint.RedactedValue);
        payload.Headers["User-Agent"].ShouldBe("EchoEndpointTests");
    }
}
