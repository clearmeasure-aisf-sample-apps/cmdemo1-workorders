using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

[TestFixture]
public class SerialLaneDurationPackTests
{
    [Test]
    public void PackedClassNames_WhenCalled_IsNonEmpty()
    {
        SerialLaneDurationPack.PackedClassNames(RepositoryCheckout.Current).Count.ShouldBeGreaterThan(0);
    }
}
