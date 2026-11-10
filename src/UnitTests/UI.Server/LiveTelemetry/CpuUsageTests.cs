using ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.LiveTelemetry;

[TestFixture]
public class CpuUsageTests
{
    [Test]
    public void ShouldReportTheShareOfAllProcessors_WhenAnIntervalWasSampled()
    {
        var cpu = new CpuUsage(0, TimeSpan.Zero, processorCount: 2, sampleMilliseconds: 2000);

        cpu.Sample(2000, TimeSpan.FromMilliseconds(1000));

        cpu.Percent.ShouldBe(25.0);
    }

    [Test]
    public void ShouldBeDue_OnlyAfterAFullIntervalOrWhenTheClockWentBack()
    {
        var cpu = new CpuUsage(10_000, TimeSpan.Zero, processorCount: 1, sampleMilliseconds: 2000);

        var early = cpu.IsDue(11_999);
        var onTime = cpu.IsDue(12_000);
        var clockWentBack = cpu.IsDue(9_000);

        early.ShouldBeFalse();
        onTime.ShouldBeTrue();
        clockWentBack.ShouldBeTrue();
    }

    [Test]
    public void ShouldClampToOneHundred_WhenMoreCpuTimeThanWallTimeIsReported()
    {
        var cpu = new CpuUsage(0, TimeSpan.Zero, processorCount: 1, sampleMilliseconds: 2000);

        cpu.Sample(1000, TimeSpan.FromSeconds(5));

        cpu.Percent.ShouldBe(100.0);
    }
}
