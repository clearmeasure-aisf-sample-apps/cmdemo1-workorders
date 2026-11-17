using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

internal static class EchoEndpointRequestSteps
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

    public static IResult InvokeGet(DefaultHttpContext httpContext)
    {
        return EchoEndpoint.Get(httpContext);
    }
}
