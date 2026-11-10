using ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;
using Shouldly;
using static ClearMeasure.Bootcamp.UI.Server.LiveTelemetry.RollingCounts;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.LiveTelemetry;

[TestFixture]
public class CounterTotalsTests
{
    [Test]
    public void ShouldReadEachSectionOfTheSnapshot_WhenTotalsAreGiven()
    {
        var raw = new int[Counter.Count];
        raw[Counter.FrontDoorTraffic] = 3;
        raw[Counter.DirectTraffic] = 4;
        raw[Counter.TrafficErrors] = 1;
        raw[Counter.Probes] = 5;
        raw[Counter.FrontDoorProbes] = 2;
        raw[Counter.SqlCommands] = 10;
        raw[Counter.SqlCommandsDuringRequests] = 7;
        raw[Counter.HttpClientCalls] = 6;
        raw[Counter.UnhandledExceptions] = 8;
        var totals = new CounterTotals(raw);

        var requests = totals.Requests(p95Ms: 120);
        var sql = totals.Sql(p95Ms: 15);

        requests.ShouldBe(new RequestCounts(7, 3, 4, 1, 120));
        totals.Probes().ShouldBe(new ProbeCounts(5, 2));
        sql.ShouldBe(new SqlCounts(10, 7, 3, 15));
        totals.HttpClient().ShouldBe(new HttpClientCounts(6));
        totals.UnhandledExceptions.ShouldBe(8);
    }
}
