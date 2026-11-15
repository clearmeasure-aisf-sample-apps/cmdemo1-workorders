using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class TestOnlyReferenceScanTests
{
    private const string SampleProduction =
        "namespace Core; public class Sample { public void OnlyInTests() { } }\n";

    private const string SampleTestCall =
        "class SampleTests { void Run() { new Sample().OnlyInTests(); } }\n";

    [Test]
    public void ShouldFlagMember_WhenOnlyTestsReferenceIt()
    {
        AssertScan(
            tree => tree.Write("src/Core/Sample.cs", SampleProduction)
                .Write("src/UnitTests/Core/SampleTests.cs", SampleTestCall),
            keys => keys.ShouldContain("src/Core/Sample.cs :: Sample.OnlyInTests()"));
    }

    [Test]
    public void ShouldPassMember_WhenProductionAlsoReferencesIt()
    {
        AssertScan(
            tree => tree.Write("src/Core/Sample.cs", SampleProduction)
                .Write("src/Core/Caller.cs", "class Caller { void Run() { new Sample().OnlyInTests(); } }\n")
                .Write("src/UnitTests/Core/SampleTests.cs", SampleTestCall),
            keys => keys.ShouldNotContain(key => key.Contains("OnlyInTests", StringComparison.Ordinal)));
    }

    [Test]
    public void ShouldFlagType_WhenOnlyTestsReferenceIt()
    {
        AssertScan(
            tree => tree.Write("src/Core/Orphan.cs", "namespace Core; public class Orphan { }\n")
                .Write("src/UnitTests/OrphanTests.cs", "class OrphanTests { void Run() { var x = new Orphan(); } }\n"),
            keys => keys.ShouldContain("src/Core/Orphan.cs :: Orphan"));
    }

    [Test]
    public void ShouldPassMember_WhenNothingReferencesIt()
    {
        AssertScan(
            tree => tree.Write("src/Core/Sample.cs", SampleProduction),
            keys => keys.ShouldNotContain(key => key.Contains("OnlyInTests", StringComparison.Ordinal)));
    }

    [Test]
    public void ShouldIgnorePrivateMember_WhenOnlyTestsReferenceIt()
    {
        AssertScan(
            tree => tree.Write("src/Core/Sample.cs", "namespace Core; public class Sample { private void Hidden() { } }\n")
                .Write("src/UnitTests/SampleTests.cs", "class SampleTests { void Run() { } }\n"),
            keys => keys.ShouldNotContain(key => key.Contains("Hidden", StringComparison.Ordinal)));
    }

    private static void AssertScan(Action<ScratchTree> arrange, Action<IReadOnlyList<string>> assert)
    {
        using var tree = new ScratchTree("test-only");
        arrange(tree);
        assert(TestOnlyReferenceScan.ScanRepository(tree.Checkout));
    }
}
