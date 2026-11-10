using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

internal static class EchoControllerRequestSteps
{
    public static DefaultHttpContext CreateGetContext()
    {
        return new DefaultHttpContext
        {
            Request =
            {
                Method = "GET",
                Path = "/api/echo"
            }
        };
    }

    public static void SetHeader(DefaultHttpContext httpContext, string name, string value) =>
        httpContext.Request.Headers[name] = value;

    public static IActionResult InvokeGet(DefaultHttpContext httpContext)
    {
        var controller = new EchoController
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        return controller.Get();
    }
}
