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
    public async Task ShouldAdvertiseConfiguredLimitAndOneSecondRetry_WhenConfigIsZero()
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var options = new ApiRateLimitingOptions { PermitLimit = 0, WindowSeconds = 0, QueueLimit = 0 };
        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await RateLimitResponseWriter.WriteRateLimitedResponseAsync(context, options);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        context.Response.Headers.RetryAfter.ToString().ShouldBe("1");
        context.Response.Headers[RateLimitingMiddleware.HeaderLimit].ToString().ShouldBe("0");
        context.Response.Headers[RateLimitingMiddleware.HeaderRemaining].ToString().ShouldBe("0");
        long.Parse(context.Response.Headers[RateLimitingMiddleware.HeaderReset].ToString())
            .ShouldBeInRange(before + 1, DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 1);
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
