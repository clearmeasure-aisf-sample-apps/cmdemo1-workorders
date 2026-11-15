using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class WorkflowJobSliceTests
{
    [Test]
    public void HasIdTokenWrite_WhenJobDeclaresIdToken_IsTrue()
    {
        var slice = new WorkflowJobSlice("upload", "  upload:\n    permissions:\n      id-token: write\n");

        slice.HasIdTokenWrite.ShouldBeTrue();
        slice.Id.ShouldBe("upload");
    }

    [Test]
    public void HasIdTokenWrite_WhenJobOmitsIdToken_IsFalse()
    {
        var slice = new WorkflowJobSlice("shards", "  shards:\n    permissions:\n      contents: read\n");

        slice.HasIdTokenWrite.ShouldBeFalse();
    }
}
