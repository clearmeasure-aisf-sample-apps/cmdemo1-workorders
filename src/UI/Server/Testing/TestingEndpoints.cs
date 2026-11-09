using ClearMeasure.Bootcamp.UI.Server.Notifications;

namespace ClearMeasure.Bootcamp.UI.Server.Testing;

/// <summary>
/// Probe endpoints that exist only in the <c>Testing</c> environment, for the web and acceptance tests of compression,
/// realtime notifications, request-body buffering, request timeouts and idempotency.
/// </summary>
public static class TestingEndpoints
{
    private const string PlainText = "text/plain; charset=utf-8";

    public static void MapTestingEndpoints(this WebApplication app)
    {
        if (!string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        app.MapGet("/_test/compression-probe", () => Results.Text(new string('A', 4096), PlainText));
        app.MapGet(
            "/_test/realtime/connection-count",
            (IRealtimeNotificationHub hub) => Results.Json(new { count = hub.ConnectionCount }));
        app.MapPost("/_test/body-buffer-probe", ReadBodyTwiceAsync);
        app.MapPost("/__test/request-body-echo", EchoRequestBodyAsync);
        app.MapGet("/api/_test/request-timeout-probe", WaitPastTheTimeoutAsync)
            .WithRequestTimeout(TimeSpan.FromMilliseconds(500));
        app.MapPost("/api/_test/idempotency-probe", CountIdempotentCalls);
        app.MapPut("/api/_test/idempotency-probe", CountIdempotentCalls);
    }

    private static async Task<IResult> ReadBodyTwiceAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        using var firstReader = new StreamReader(request.Body, leaveOpen: true);
        var first = await firstReader.ReadToEndAsync(cancellationToken);
        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        using var secondReader = new StreamReader(request.Body, leaveOpen: true);
        var second = await secondReader.ReadToEndAsync(cancellationToken);
        return Results.Json(new { first, second });
    }

    private static async Task EchoRequestBodyAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = PlainText;
        using var reader = new StreamReader(httpContext.Request.Body);
        await httpContext.Response.WriteAsync(await reader.ReadToEndAsync());
    }

    private static async Task<IResult> WaitPastTheTimeoutAsync(CancellationToken requestAborted)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), requestAborted);
        return Results.Ok();
    }

    private static IResult CountIdempotentCalls(IdempotencyProbeState state) =>
        Results.Text($"count={state.Next()}", PlainText);
}
