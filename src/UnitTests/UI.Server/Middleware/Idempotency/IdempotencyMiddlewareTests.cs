using System.Text;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Middleware.Idempotency;

/// <summary>
/// <see cref="IdempotencyMiddleware"/> against a <see cref="DefaultHttpContext"/>, with no app host.
/// </summary>
[TestFixture]
public class IdempotencyMiddlewareTests
{
    private int _nextCalls;

    [SetUp]
    public void ResetNextCalls()
    {
        _nextCalls = 0;
    }

    [Test]
    public async Task ShouldCallNext_WhenTheRequestHasNoIdempotencyKey()
    {
        var middleware = CreateMiddleware(maxKeyLength: 256);
        var context = Post("/api/items", "{}", key: null);

        await middleware.InvokeAsync(context);

        _nextCalls.ShouldBe(1);
        context.Response.StatusCode.ShouldBe(StatusCodes.Status201Created);
    }

    [Test]
    public async Task ShouldReturn400_WhenTheKeyIsLongerThanTheMaximum()
    {
        var middleware = CreateMiddleware(maxKeyLength: 4);
        var context = Post("/api/items", "{}", key: "too-long-key");

        await middleware.InvokeAsync(context);

        _nextCalls.ShouldBe(0);
        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        context.Response.ContentType.ShouldBe("application/problem+json");
    }

    [Test]
    public async Task ShouldReplayTheFirstResponse_WhenTheSameRequestIsSentAgain()
    {
        var middleware = CreateMiddleware(maxKeyLength: 256);
        var first = Post("/api/items", "{\"name\":\"a\"}", key: "key-1");
        var second = Post("/api/items", "{\"name\":\"a\"}", key: "key-1");

        await middleware.InvokeAsync(first);
        await middleware.InvokeAsync(second);

        _nextCalls.ShouldBe(1);
        second.Response.StatusCode.ShouldBe(StatusCodes.Status201Created);
        ResponseText(second).ShouldBe("created");
        ResponseText(first).ShouldBe("created");
    }

    [Test]
    public async Task ShouldReturn409_WhenTheKeyIsReusedWithADifferentBody()
    {
        var middleware = CreateMiddleware(maxKeyLength: 256);
        var first = Post("/api/items", "{\"name\":\"a\"}", key: "key-2");
        var reused = Post("/api/items", "{\"name\":\"b\"}", key: "key-2");

        await middleware.InvokeAsync(first);
        await middleware.InvokeAsync(reused);

        _nextCalls.ShouldBe(1);
        reused.Response.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        reused.Response.ContentType.ShouldBe("application/problem+json");
    }

    private IdempotencyMiddleware CreateMiddleware(int maxKeyLength)
    {
        var options = new ServiceCollection()
            .Configure<IdempotencyOptions>(o => o.MaxKeyLength = maxKeyLength)
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<IdempotencyOptions>>();
        return new IdempotencyMiddleware(RespondCreated, new MemoryCache(new MemoryCacheOptions()), options);
    }

    private async Task RespondCreated(HttpContext context)
    {
        _nextCalls++;
        context.Response.StatusCode = StatusCodes.Status201Created;
        await context.Response.Body.WriteAsync("created"u8.ToArray());
    }

    private static DefaultHttpContext Post(string path, string body, string? key)
    {
        return new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Post,
                Path = path,
                Body = new MemoryStream(Encoding.UTF8.GetBytes(body)),
                Headers = { [IdempotencyConstants.HeaderName] = key }
            },
            Response = { Body = new MemoryStream() }
        };
    }

    private static string ResponseText(HttpContext context) =>
        Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());
}
