using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class ScratchTreeTests
{
    [Test]
    public void Write_WhenFoldersAreMissing_CreatesThemAndTheFile()
    {
        using var tree = new ScratchTree("scratch-write");

        tree.Write("src/Core/Model/WorkOrder.cs", "class WorkOrder;");

        File.ReadAllText(tree.PathOf("src/Core/Model/WorkOrder.cs")).ShouldBe("class WorkOrder;");
    }

    [Test]
    public void Dispose_WhenTreeHoldsAReadOnlyFile_DeletesTheWholeTree()
    {
        var tree = TreeWithAReadOnlyFile();

        tree.Dispose();

        Directory.Exists(tree.Root).ShouldBeFalse();
    }

    [Test]
    public void Checkout_WhenRead_IsRootedAtTheTree()
    {
        using var tree = new ScratchTree("scratch-checkout");

        tree.Checkout.Root.ShouldBe(Path.GetFullPath(tree.Root));
    }

    private static ScratchTree TreeWithAReadOnlyFile()
    {
        var tree = new ScratchTree("scratch-read-only").Write(".git/objects/ab/cdef", "blob");
        File.SetAttributes(tree.PathOf(".git/objects/ab/cdef"), FileAttributes.ReadOnly);
        return tree;
    }
}
