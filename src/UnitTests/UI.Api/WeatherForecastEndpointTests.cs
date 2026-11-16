using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class WeatherForecastEndpointTests
{
    [Test]
    public void Get_Should_ReturnOk_WithFiveForecastsAndEtag()
    {
        var endpoint = CreateEndpoint();

        var result = endpoint.Get();

        var content = result.ShouldBeContentResult();
        content.StatusCode.ShouldBe(StatusCodes.Status200OK);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("application/json");
        var forecasts = JsonSerializer.Deserialize<WeatherForecast[]>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        forecasts.ShouldNotBeNull();
        forecasts.Length.ShouldBe(5);
        endpoint.Response.Headers.ETag.ToString().ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchIsAny()
    {
        var endpoint = CreateEndpoint();
        endpoint.Request.Headers.IfNoneMatch = "*";

        var result = endpoint.Get();

        var status = result.ShouldBeStatusCodeResult();
        status.StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }

    private static EndpointUnderTest CreateEndpoint() =>
        new(context => WeatherForecastEndpoint.Get(context, NullLoggerFactory.Instance));

    private sealed class EndpointUnderTest(Func<HttpContext, IResult> get)
    {
        public HttpContext HttpContext { get; } = new DefaultHttpContext();
        public HttpRequest Request => HttpContext.Request;
        public HttpResponse Response => HttpContext.Response;
        public IResult Get() => get(HttpContext);
    }
}
