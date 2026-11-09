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
        using var tree = SourceTree(("A.cs", Block), ("B.cs", Block));

        var scan = DuplicationGate.Scan(tree.Checkout);

        scan.DuplicatedLines.ShouldBe(16);
        scan.TotalLines.ShouldBe(16);
        scan.MinLines.ShouldBe(DuplicationGate.MinLines);
    }

    [Test]
    public void Scan_WhenBlockIsUnique_CountsZero()
    {
        using var tree = SourceTree(("A.cs", Block));

        DuplicationGate.Scan(tree.Checkout).DuplicatedLines.ShouldBe(0);
    }

    [Test]
    public void Scan_WhenRepeatIsShorterThanMinimum_CountsZero()
    {
        var shortBlock = string.Join('\n', Block.Split('\n').Take(7));
        using var tree = SourceTree(("A.cs", shortBlock), ("B.cs", shortBlock));

        DuplicationGate.Scan(tree.Checkout).DuplicatedLines.ShouldBe(0);
    }

    [Test]
    public void Scan_WhenRepeatIsUnderGenerated_IgnoresIt()
    {
        using var tree = SourceTree(("Generated/A.cs", Block), ("Generated/B.cs", Block), ("Unique.cs", "only one line"));

        DuplicationGate.Scan(tree.Checkout).DuplicatedLines.ShouldBe(0);
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
        var checkout = RepositoryCheckout.Current;
        var scan = DuplicationGate.Scan(checkout);
        var ceiling = DuplicationGate.ReadMaxDuplicatedLines(checkout);

        DuplicationGate.EnsureWithinCeiling(scan.DuplicatedLines, ceiling);
        scan.DuplicatedLines.ShouldBeLessThanOrEqualTo(ceiling);
    }

    private static ScratchTree SourceTree(params (string Path, string Contents)[] files)
    {
        var tree = new ScratchTree("dup-gate");
        foreach (var (path, contents) in files)
        {
            tree.Write("src/" + path, contents);
        }

        return tree;
    }
}
