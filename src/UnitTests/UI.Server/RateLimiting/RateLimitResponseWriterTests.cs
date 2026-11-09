using System.Text;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.RateLimiting;

[TestFixture]
public class RateLimitResponseWriterTests
{
    [Test]
    public async Task ShouldAnswerTooManyRequestsWithRetryAfter_WhenARequestIsRefused()
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var options = new ApiRateLimitingOptions { PermitLimit = 100, WindowSeconds = 60 };

        await RateLimitResponseWriter.WriteRateLimitedResponseAsync(context, options);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        context.Response.Headers.RetryAfter.ToString().ShouldBe("60");
        context.Response.Headers[RateLimitingMiddleware.HeaderLimit].ToString().ShouldBe("100");
        context.Response.Headers[RateLimitingMiddleware.HeaderRemaining].ToString().ShouldBe("0");
    }

    [Test]
    public async Task ShouldExplainTheRefusal_WhenTheResponseHasNotStarted()
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await RateLimitResponseWriter.WriteRateLimitedResponseAsync(context, new ApiRateLimitingOptions());

        context.Response.ContentType.ShouldBe("text/plain; charset=utf-8");
        Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray())
            .ShouldBe("Too many requests. Please try again later.");
    }
}
