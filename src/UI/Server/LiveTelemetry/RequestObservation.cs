using Microsoft.AspNetCore.Diagnostics;

namespace ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;

/// <summary>
/// What <see cref="LiveTelemetryMiddleware"/> learns about one request while the rest of the pipeline runs: its kind,
/// when it started, the status it ended with and whether it ended in an unhandled exception.
/// </summary>
internal sealed class RequestObservation(RequestKind kind, bool isWebSocket, long startedTimestamp)
{
    private int _statusCode = StatusCodes.Status500InternalServerError;
    private bool _endedInException;

    public static RequestObservation Start(HttpContext context, TimeProvider timeProvider) =>
        new(RequestClassifier.Classify(context.Request), context.WebSockets.IsWebSocketRequest, timeProvider.GetTimestamp());

    /// <summary>
    /// The pipeline returned. The exception handler (it runs after the middleware) leaves its feature behind when it
    /// turned an exception into the response.
    /// </summary>
    public void Completed(HttpContext context)
    {
        _statusCode = context.Response.StatusCode;
        _endedInException = context.Features.Get<IExceptionHandlerFeature>() is not null;
    }

    /// <summary>The client went away: not an unhandled exception.</summary>
    public void Aborted(HttpContext context) => _statusCode = context.Response.StatusCode;

    public void Failed() => _endedInException = true;

    /// <summary>Records the request; a WebSocket's duration is not a response time, so it has none.</summary>
    public void RecordIn(LiveTelemetryCounters counters, TimeProvider timeProvider)
    {
        if (_endedInException)
        {
            counters.RecordUnhandledException();
        }

        counters.RecordRequest(kind, _statusCode, isWebSocket ? null : timeProvider.GetElapsedTime(startedTimestamp));
    }
}
