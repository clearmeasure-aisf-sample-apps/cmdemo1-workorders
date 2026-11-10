using System.Collections.Concurrent;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Replays the first successful (2xx) response for duplicate POST or PUT requests to <c>/api/*</c> (and the Blazor WASM
/// single-API paths) that share the same <see cref="IdempotencyConstants.HeaderName"/> and the same method, path, and body.
/// </summary>
public sealed class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly IOptionsMonitor<IdempotencyOptions> _optionsMonitor;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new(StringComparer.Ordinal);

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public IdempotencyMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        IOptionsMonitor<IdempotencyOptions> optionsMonitor)
    {
        _next = next;
        _cache = cache;
        _optionsMonitor = optionsMonitor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IdempotentRequest.ShouldInspect(context.Request) || IdempotentRequest.ReadKey(context.Request) is not { } key)
        {
            await _next(context);
            return;
        }

        var options = _optionsMonitor.CurrentValue;
        if (key.Length > options.MaxKeyLength)
        {
            await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status400BadRequest, "Bad Request",
                $"Idempotency key exceeds maximum length of {options.MaxKeyLength}.");
            return;
        }

        var request = await IdempotentRequest.ForAsync(context.Request, key, context.RequestAborted);
        var store = new IdempotencyStore(_cache, TimeSpan.FromSeconds(Math.Max(1, options.CacheEntrySeconds)));
        if (store.Find(request) is { } cached)
        {
            await cached.ReplayAsync(context);
            return;
        }

        await HandleOnceAsync(context, request, store);
    }

    /// <summary>One request per key at a time: replay, refuse a reused key, or run the request and remember it.</summary>
    private async Task HandleOnceAsync(HttpContext context, IdempotentRequest request, IdempotencyStore store)
    {
        var keyLock = _keyLocks.GetOrAdd(request.Key, static _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync(context.RequestAborted);
        try
        {
            if (store.Find(request) is { } cached)
            {
                await cached.ReplayAsync(context);
            }
            else if (store.IsKeyUsedForAnotherRequest(request))
            {
                await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status409Conflict, "Conflict",
                    "This Idempotency-Key was already used with a different request payload.");
            }
            else
            {
                await ForwardAndRememberAsync(context, request, store);
            }
        }
        finally
        {
            keyLock.Release();
        }
    }

    private async Task ForwardAndRememberAsync(HttpContext context, IdempotentRequest request, IdempotencyStore store)
    {
        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await _next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        if (context.Response.StatusCode is >= 200 and < 300)
        {
            store.Remember(request, IdempotentResponseSnapshot.Capture(context.Response, buffer.ToArray()));
        }

        buffer.Position = 0;
        context.Response.ContentLength = buffer.Length;
        await buffer.CopyToAsync(originalBody, context.RequestAborted);
    }
}
