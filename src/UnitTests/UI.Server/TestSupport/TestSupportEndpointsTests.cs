using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class TestSupportEndpointsTests
{
    [Test]
    public void ShouldExposeMapMethod_WhenTypeIsLoaded()
    {
        typeof(TestSupportEndpoints).GetMethod(nameof(TestSupportEndpoints.MapTestSupportEndpoints)).ShouldNotBeNull();
    }
}
