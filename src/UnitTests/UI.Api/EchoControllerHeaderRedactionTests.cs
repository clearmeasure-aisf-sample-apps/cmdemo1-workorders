using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EchoControllerHeaderRedactionTests
{
    [Test]
    public void Get_Should_RedactAuthorizationApiKeyAndCookie()
    {
        var httpContext = EchoControllerRequestSteps.CreateGetContext();
        EchoControllerRequestSteps.SetHeader(httpContext, "Authorization", "Bearer secret-token");
        EchoControllerRequestSteps.SetHeader(httpContext, "X-Api-Key", "api-key-value");
        EchoControllerRequestSteps.SetHeader(httpContext, "Cookie", "session=abc");
        EchoControllerRequestSteps.SetHeader(httpContext, "Accept", "application/json");

        var result = EchoControllerRequestSteps.InvokeGet(httpContext);

        var payload = result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<EchoResponse>();
        payload.Headers["Authorization"].ShouldBe(EchoController.RedactedValue);
        payload.Headers["X-Api-Key"].ShouldBe(EchoController.RedactedValue);
        payload.Headers["Cookie"].ShouldBe(EchoController.RedactedValue);
        payload.Headers["Accept"].ShouldBe("application/json");
    }

    [Test]
    public void Get_Should_RedactSetCookieHeader()
    {
        var httpContext = EchoControllerRequestSteps.CreateGetContext();
        EchoControllerRequestSteps.SetHeader(httpContext, "Set-Cookie", "auth=super-secret; Path=/; HttpOnly");
        EchoControllerRequestSteps.SetHeader(httpContext, "User-Agent", "EchoControllerTests");

        var result = EchoControllerRequestSteps.InvokeGet(httpContext);

        var payload = result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<EchoResponse>();
        payload.Headers["Set-Cookie"].ShouldBe(EchoController.RedactedValue);
        payload.Headers["User-Agent"].ShouldBe("EchoControllerTests");
    }
}
