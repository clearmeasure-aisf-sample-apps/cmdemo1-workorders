using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderReportsExporterTests
{
    private static WorkOrderReportsData Sample() => new()
    {
        CountByStatus = { ["InProgress"] = 1, ["Draft"] = 2 },
        CountByPriority = { ["Low"] = 1, ["High"] = 3 },
        CountByBuilding = { ["Kitchen"] = 2, ["Sanctuary"] = 4 },
        CompletedPerWeek = [new WorkOrderReportsData.WeeklyCount { WeekEndingDate = new DateOnly(2026, 10, 11), Count = 5 }],
        AverageDaysToClose = 2.54,
        OverdueCount = 7,
        CompletedLast30Days = 9
    };

    [Test]
    public void ShouldWriteSectionsInFixedOrder()
    {
        var lines = WorkOrderReportsExporter.ToCsv(Sample()).Split("\r\n");

        lines.ShouldContain("Open by Status,Draft,2");
        lines.ShouldContain("Open by Status,InProgress,1");
        lines.ShouldNotContain("Open by Status,Assigned,0");
        Array.IndexOf(lines, "Open by Status,Draft,2").ShouldBeLessThan(Array.IndexOf(lines, "Open by Status,InProgress,1"));
        Array.IndexOf(lines, "Open by Priority,High,3").ShouldBeLessThan(Array.IndexOf(lines, "Open by Priority,Low,1"));
        Array.IndexOf(lines, "Open by Building,Sanctuary,4").ShouldBeLessThan(Array.IndexOf(lines, "Open by Building,Kitchen,2"));
    }

    [Test]
    public void ShouldWriteWeeksAndSummary()
    {
        var lines = WorkOrderReportsExporter.ToCsv(Sample()).Split("\r\n");

        lines.ShouldContain("2026-10-11,5");
        lines.ShouldContain("Completed (30 days),9");
        lines.ShouldContain("Average Days to Close,2.5");
        lines.ShouldContain("Overdue Count,7");
    }

    [Test]
    public void ShouldWriteNotAvailableWhenNoAverage()
    {
        var data = Sample();
        data.AverageDaysToClose = null;

        WorkOrderReportsExporter.ToCsv(data).ShouldContain("Average Days to Close,N/A");
    }
}
