using System.Globalization;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TimeControllerTests
{
    [Test]
    public void Get_Should_ReturnPlainTextIso8601Utc_ForFixedClock()
    {
        var fixedUtc = new DateTime(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc);
        var stubTimeProvider = new StubFixedUtcTimeProvider(fixedUtc);
        var controller = new TimeController(stubTimeProvider)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.StatusCode.ShouldBe(200);
        content.ContentType.ShouldNotBeNull();
        content.ContentType.ShouldContain("text/plain");
        var expected = new DateTimeOffset(fixedUtc, TimeSpan.Zero).ToString("O", CultureInfo.InvariantCulture);
        content.Content.ShouldBe(expected);
        controller.Response.Headers[HeaderNames.CacheControl].ToString().ShouldBe("no-store");
        controller.Response.Headers.ETag.ToString().ShouldNotBeNullOrEmpty();
    }

    [Test]
    public void Get_Should_Return304WithNoStore_When_IfNoneMatchMatchesEtag()
    {
        var fixedUtc = new DateTime(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc);
        var stubTimeProvider = new StubFixedUtcTimeProvider(fixedUtc);
        var expected = new DateTimeOffset(fixedUtc, TimeSpan.Zero).ToString("O", CultureInfo.InvariantCulture);
        var etag = ConditionalGetEtag.CreateWeakEtagForPlainText(expected);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfNoneMatch = etag.ToString();
        var controller = new TimeController(stubTimeProvider)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = controller.Get();

        result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(StatusCodes.Status304NotModified);
        controller.Response.Headers[HeaderNames.CacheControl].ToString().ShouldBe("no-store");
        controller.Response.Headers.ETag.ToString().ShouldBe(etag.ToString());
    }

    private sealed class StubFixedUtcTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
