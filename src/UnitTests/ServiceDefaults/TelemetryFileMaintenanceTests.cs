using ChurchBulletin.ServiceDefaults;
using Shouldly;
using static ClearMeasure.Bootcamp.UnitTests.ServiceDefaults.TelemetryTestFiles;

namespace ClearMeasure.Bootcamp.UnitTests.ServiceDefaults;

[TestFixture]
public class TelemetryFileMaintenanceTests
{
    [Test]
    public void NextSequencePath_WithExistingSequences_ShouldUseHighestPlusOne()
    {
        var directory = CreateTempDirectory();
        var basePath = Path.Combine(directory, "logs_d.jsonl");
        File.WriteAllText(Path.Combine(directory, "logs_d_0.jsonl"), "{}");
        File.WriteAllText(Path.Combine(directory, "logs_d_3.jsonl"), "{}");
        File.WriteAllText(Path.Combine(directory, "logs_d_x.jsonl"), "{}");
        File.WriteAllText(Path.Combine(directory, "metrics_d_9.jsonl"), "{}");

        TelemetryFileMaintenance.NextSequencePath(basePath).ShouldBe(Path.Combine(directory, "logs_d_4.jsonl"));
    }

    [Test]
    public void NextSequencePath_WithoutSequences_ShouldStartAtZero()
    {
        var directory = CreateTempDirectory();
        var basePath = Path.Combine(directory, "logs_d.jsonl");

        TelemetryFileMaintenance.NextSequencePath(basePath).ShouldBe(Path.Combine(directory, "logs_d_0.jsonl"));
    }

    [Test]
    public void TryMoveToNextSequence_WhenFileMissing_ShouldNotThrow()
    {
        var basePath = Path.Combine(CreateTempDirectory(), "missing", "logs_d.jsonl");

        Should.NotThrow(() => TelemetryFileMaintenance.TryMoveToNextSequence(basePath));
    }

    [Test]
    public void TryMoveToNextSequence_WhenNextSequenceAppearsBeforeMove_ShouldKeepFileAndNotThrow()
    {
        var directory = CreateTempDirectory();
        var basePath = CreateJsonl(directory, "logs_d.jsonl", FixedNow.UtcDateTime);
        var takenPath = Path.Combine(directory, "logs_d_0.jsonl");
        Directory.CreateDirectory(takenPath);
        TelemetryFileMaintenance.NextSequencePath(basePath).ShouldBe(takenPath);

        Should.NotThrow(() => TelemetryFileMaintenance.TryMoveToNextSequence(basePath));

        File.Exists(basePath).ShouldBeTrue();
        File.ReadAllText(basePath).ShouldBe("{}");
        File.Exists(takenPath).ShouldBeFalse();
        Directory.Exists(takenPath).ShouldBeTrue();
    }

    [Test]
    public void DeleteFilesOlderThan_ShouldDeleteOldKeepNewAndKeepPaths()
    {
        var directory = CreateTempDirectory();
        var old = FixedNow.UtcDateTime.AddDays(-10);
        var oldFile = CreateJsonl(directory, "logs_2026-01-05.jsonl", old);
        var recentFile = CreateJsonl(directory, "logs_2026-01-14.jsonl", FixedNow.UtcDateTime.AddDays(-1));
        var openFile = CreateJsonl(directory, "metrics_2026-01-05.jsonl", old);
        var textFile = CreateJsonl(directory, "notes.txt", old);

        TelemetryFileMaintenance.DeleteFilesOlderThan(directory, 3, FixedUtcClock.At(FixedNow), [openFile]);

        File.Exists(oldFile).ShouldBeFalse();
        File.Exists(recentFile).ShouldBeTrue();
        File.Exists(openFile).ShouldBeTrue();
        File.Exists(textFile).ShouldBeTrue();
    }

    [Test]
    public void DeleteFilesOlderThan_WhenKeepPathOlderThanCutoff_ShouldKeepIt()
    {
        var directory = CreateTempDirectory();
        var keptFile = CreateJsonl(directory, "logs_2026-01-01.jsonl", FixedNow.UtcDateTime.AddDays(-14));
        var expiredFile = CreateJsonl(directory, "events_2026-01-01.jsonl", FixedNow.UtcDateTime.AddDays(-14));

        TelemetryFileMaintenance.DeleteFilesOlderThan(directory, 3, FixedUtcClock.At(FixedNow), [keptFile]);

        File.Exists(keptFile).ShouldBeTrue();
        File.Exists(expiredFile).ShouldBeFalse();
    }

    [Test]
    public void DeleteFilesOlderThan_WhenDirectoryMissing_ShouldNotThrow()
    {
        var directory = Path.Combine(CreateTempDirectory(), "missing");

        Should.NotThrow(() =>
            TelemetryFileMaintenance.DeleteFilesOlderThan(directory, 3, FixedUtcClock.At(FixedNow), []));
    }
}
