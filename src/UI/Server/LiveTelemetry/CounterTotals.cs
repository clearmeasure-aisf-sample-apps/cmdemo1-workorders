using static ClearMeasure.Bootcamp.UI.Server.LiveTelemetry.RollingCounts;

namespace ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;

/// <summary>
/// The counters of one window summed, read as the sections of a <see cref="LiveTelemetrySnapshot"/>.
/// </summary>
internal readonly record struct CounterTotals(int[] Totals)
{
    public int UnhandledExceptions => Totals[Counter.UnhandledExceptions];

    public RequestCounts Requests(int? p95Ms)
    {
        var frontDoor = Totals[Counter.FrontDoorTraffic];
        var direct = Totals[Counter.DirectTraffic];
        return new RequestCounts(frontDoor + direct, frontDoor, direct, Totals[Counter.TrafficErrors], p95Ms);
    }

    public ProbeCounts Probes() => new(Totals[Counter.Probes], Totals[Counter.FrontDoorProbes]);

    public SqlCounts Sql(int? p95Ms)
    {
        var all = Totals[Counter.SqlCommands];
        var duringRequests = Totals[Counter.SqlCommandsDuringRequests];
        return new SqlCounts(all, duringRequests, all - duringRequests, p95Ms);
    }

    public HttpClientCounts HttpClient() => new(Totals[Counter.HttpClientCalls]);
}
