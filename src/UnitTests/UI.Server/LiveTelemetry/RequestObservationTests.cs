using ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.LiveTelemetry;

[TestFixture]
public class RequestObservationTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 5, 23, 0, 0, TimeSpan.Zero);

    [Test]
    public void ShouldRecordTheStatusAndDuration_WhenTheRequestCompleted()
    {
        var clock = new StubTimeProvider(Start);
        var counters = new LiveTelemetryCounters(clock, new StubProcessMeter());
        var context = new DefaultHttpContext { Request = { Path = "/work-orders" } };
        var observation = RequestObservation.Start(context, clock);
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        clock.Advance(TimeSpan.FromMilliseconds(40));

        observation.Completed(context);
        observation.RecordIn(counters, clock);

        var requests = counters.Snapshot().Requests;
        requests.PerMinute.ShouldBe(1);
        requests.Errors.ShouldBe(1);
        requests.P95Ms.ShouldBe(40);
        counters.Snapshot().Process.ExceptionsPerMinute.ShouldBe(0);
    }

    [Test]
    public void ShouldCountAnUnhandledException_WhenTheExceptionHandlerAnswered()
    {
        var clock = new StubTimeProvider(Start);
        var counters = new LiveTelemetryCounters(clock, new StubProcessMeter());
        var context = new DefaultHttpContext { Request = { Path = "/work-orders" } };
        context.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature { Error = new InvalidOperationException() });
        var observation = RequestObservation.Start(context, clock);

        observation.Completed(context);
        observation.RecordIn(counters, clock);

        counters.Snapshot().Process.ExceptionsPerMinute.ShouldBe(1);
    }

    [Test]
    public void ShouldCountNoException_WhenTheClientAborted()
    {
        var clock = new StubTimeProvider(Start);
        var counters = new LiveTelemetryCounters(clock, new StubProcessMeter());
        var context = new DefaultHttpContext { Request = { Path = "/work-orders" } };
        var observation = RequestObservation.Start(context, clock);
        context.Response.StatusCode = StatusCodes.Status200OK;

        observation.Aborted(context);
        observation.RecordIn(counters, clock);

        counters.Snapshot().Process.ExceptionsPerMinute.ShouldBe(0);
        counters.Snapshot().Requests.Errors.ShouldBe(0);
    }

    [Test]
    public void ShouldCountAFailure_WhenAnExceptionEscapedThePipeline()
    {
        var clock = new StubTimeProvider(Start);
        var counters = new LiveTelemetryCounters(clock, new StubProcessMeter());
        var observation = RequestObservation.Start(new DefaultHttpContext { Request = { Path = "/" } }, clock);

        observation.Failed();
        observation.RecordIn(counters, clock);

        counters.Snapshot().Process.ExceptionsPerMinute.ShouldBe(1);
        counters.Snapshot().Requests.Errors.ShouldBe(1);
    }
}
