using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class RepositoryCheckoutTests
{
    [Test]
    public void Current_WhenTestsRunFromBuildOutput_IsTheDirectoryHoldingTheSolution()
    {
        var checkout = RepositoryCheckout.Current;

        File.Exists(checkout.PathOf("src/ChurchBulletin.sln")).ShouldBeTrue(checkout.Root);
        TestContext.CurrentContext.TestDirectory.ShouldStartWith(checkout.Root);
    }

    [Test]
    public void SourceFiles_WhenTreeHasBuildOutputAndGeneratedCode_ListsOnlyHandWrittenFilesInOrder()
    {
        using var tree = RepositoryCheckoutMother.TreeWithEveryKindOfFile();

        var files = tree.Checkout.SourceFiles(".cs", ".razor").Select(tree.Checkout.Relative);

        files.ShouldBe(RepositoryCheckoutMother.HandWrittenSources);
    }

    [Test]
    public void SourceFiles_WhenCheckoutHasNoSrcFolder_IsEmpty()
    {
        using var tree = new ScratchTree("checkout-without-src").Write("README.md", "# Empty");

        tree.Checkout.SourceFiles(".cs").ShouldBeEmpty();
    }

    [TestCaseSource(typeof(RepositoryCheckoutMother), nameof(RepositoryCheckoutMother.NotHandWritten))]
    public void IsHandWritten_WhenPathIsBuildOutputOrGenerated_IsFalse(string relativePath)
    {
        RepositoryCheckout.IsHandWritten(relativePath).ShouldBeFalse();
    }

    [Test]
    public void IsHandWritten_WhenPathIsOrdinarySource_IsTrue()
    {
        RepositoryCheckout.IsHandWritten("src/Core/Model/WorkOrder.cs").ShouldBeTrue();
    }

    [Test]
    public void ReadText_WhenFileIsMissing_NamesTheRelativePath()
    {
        using var tree = new ScratchTree("checkout-missing-file");

        var error = Should.Throw<FileNotFoundException>(() => tree.Checkout.ReadText("docs/missing.md"));

        error.Message.ShouldContain("docs/missing.md");
    }

    [Test]
    public void ReadRatchet_WhenBaselineIsCommitted_ParsesItsKeys()
    {
        using var tree = new ScratchTree("checkout-ratchet").Write("gate/baseline.txt", RatchetKeysMother.CommentedBaselineText());

        var ratchet = tree.Checkout.ReadRatchet("gate/baseline.txt");

        ratchet.Keys.ShouldBe([RatchetKeysMother.WideFile, RatchetKeysMother.LongMethod]);
    }

    [Test]
    public void Relative_WhenGivenAFullPath_UsesForwardSlashes()
    {
        using var tree = new ScratchTree("checkout-relative");

        tree.Checkout.Relative(tree.PathOf("src/Core/Model/WorkOrder.cs")).ShouldBe("src/Core/Model/WorkOrder.cs");
    }
}
