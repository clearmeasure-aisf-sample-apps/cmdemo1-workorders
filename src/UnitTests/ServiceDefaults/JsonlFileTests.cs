using ChurchBulletin.ServiceDefaults;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.ServiceDefaults;

[TestFixture]
public class JsonlFileTests
{
    private string _directory = null!;

    [SetUp]
    public void CreateTemporaryDirectory()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"jsonl-file-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void DeleteTemporaryDirectory() => Directory.Delete(_directory, recursive: true);

    [Test]
    public void ShouldAppendOneLinePerWrite_WhenTheFileIsBelowItsCap()
    {
        var path = Path.Combine(_directory, "traces.jsonl");

        using (var file = new JsonlFile(path, maxBytes: 1024, onRoll: () => { }))
        {
            file.WriteLine("{\"a\":1}");
            file.WriteLine("{\"b\":2}");
        }

        File.ReadAllLines(path).ShouldBe(["{\"a\":1}", "{\"b\":2}"]);
    }

    [Test]
    public void ShouldRollToANewFile_WhenTheCapIsReached()
    {
        var path = Path.Combine(_directory, "logs.jsonl");
        var rolls = 0;

        using (var file = new JsonlFile(path, maxBytes: 4, onRoll: () => rolls++))
        {
            file.WriteLine("first-line");
            file.WriteLine("second");
        }

        rolls.ShouldBe(1);
        File.ReadAllLines(path).ShouldBe(["second"]);
        Directory.GetFiles(_directory).Length.ShouldBe(2);
    }

    [Test]
    public void ShouldIgnoreWrites_WhenTheFileWasDisposed()
    {
        var path = Path.Combine(_directory, "events.jsonl");
        var file = new JsonlFile(path, maxBytes: 1024, onRoll: () => { });
        file.Dispose();

        file.WriteLine("late");

        File.ReadAllText(path).ShouldBeEmpty();
    }
}
