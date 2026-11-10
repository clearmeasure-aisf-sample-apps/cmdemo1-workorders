namespace ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;

/// <summary>
/// Classifies each request with <see cref="RequestClassifier"/> and records it, with its status code and duration,
/// in <see cref="LiveTelemetryCounters"/> once the rest of the pipeline has run. While the pipeline runs, the
/// request counts as executing and its flow is marked (<see cref="RequestFlow"/>).
/// </summary>
public sealed class LiveTelemetryMiddleware(RequestDelegate next, LiveTelemetryCounters counters, TimeProvider timeProvider)
{
    /// <summary>
    /// Invokes the next middleware and records the request.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var observation = RequestObservation.Start(context, timeProvider);
        counters.RequestStarted();
        var flow = RequestFlow.Begin();
        try
        {
            await next(context);
            observation.Completed(context);
        }
        catch when (context.RequestAborted.IsCancellationRequested)
        {
            observation.Aborted(context);
            throw;
        }
        catch
        {
            observation.Failed();
            throw;
        }
        finally
        {
            flow.End();
            counters.RequestEnded();
            observation.RecordIn(counters, timeProvider);
        }
    }
}
