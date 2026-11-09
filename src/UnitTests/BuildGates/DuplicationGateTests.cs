using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class DuplicationGateTests
{
    private const string Block = """
        alpha one
        beta two
        gamma three
        delta four
        epsilon five
        zeta six
        eta seven
        theta eight
        """;

    [Test]
    public void Scan_WhenSameBlockAppearsInTwoFiles_CountsThoseLines()
    {
        using var dir = new TempTree();
        dir.Write("A.cs", Block);
        dir.Write("B.cs", Block);

        var scan = DuplicationGate.Scan(dir.Root);

        scan.DuplicatedLines.ShouldBe(16);
        scan.TotalLines.ShouldBe(16);
        scan.MinLines.ShouldBe(DuplicationGate.MinLines);
    }

    [Test]
    public void Scan_WhenBlockIsUnique_CountsZero()
    {
        using var dir = new TempTree();
        dir.Write("A.cs", Block);

        DuplicationGate.Scan(dir.Root).DuplicatedLines.ShouldBe(0);
    }

    [Test]
    public void Scan_WhenRepeatIsShorterThanMinimum_CountsZero()
    {
        using var dir = new TempTree();
        var shortBlock = string.Join('\n', Block.Split('\n').Take(7));
        dir.Write("A.cs", shortBlock);
        dir.Write("B.cs", shortBlock);

        DuplicationGate.Scan(dir.Root).DuplicatedLines.ShouldBe(0);
    }

    [Test]
    public void Scan_WhenRepeatIsUnderGenerated_IgnoresIt()
    {
        using var dir = new TempTree();
        dir.Write(Path.Combine("Generated", "A.cs"), Block);
        dir.Write(Path.Combine("Generated", "B.cs"), Block);
        dir.Write("Unique.cs", "only one line");

        DuplicationGate.Scan(dir.Root).DuplicatedLines.ShouldBe(0);
    }

    [Test]
    public void EnsureWithinCeiling_WhenMeasuredEqualsCeiling_Passes()
    {
        DuplicationGate.EnsureWithinCeiling(duplicatedLines: 4, ceiling: 4);
    }

    [Test]
    public void EnsureWithinCeiling_WhenMeasuredExceedsCeiling_Fails()
    {
        var error = Should.Throw<InvalidOperationException>(
            () => DuplicationGate.EnsureWithinCeiling(duplicatedLines: 5, ceiling: 4));

        error.Message.ShouldContain("5");
        error.Message.ShouldContain("4");
    }

    [Test]
    public void Src_DuplicatedLines_DoNotExceedBaseline()
    {
        var root = RepoFiles.Root();
        var scan = DuplicationGate.Scan(Path.Combine(root, "src"));
        var ceiling = DuplicationGate.ReadMaxDuplicatedLines(
            Path.Combine(root, "scripts", "dead-code", "duplication-baseline.json"));

        DuplicationGate.EnsureWithinCeiling(scan.DuplicatedLines, ceiling);
        scan.DuplicatedLines.ShouldBeLessThanOrEqualTo(ceiling);
    }

    private sealed class TempTree : IDisposable
    {
        public TempTree()
        {
            Root = Path.Combine(Path.GetTempPath(), "dup-gate-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Write(string relative, string contents)
        {
            var path = Path.Combine(Root, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
