using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class TestSupportMiddlewareTests
{
    [TestCase("/api/test-support")]
    [TestCase("/api/test-support/employees")]
    [TestCase("/API/Test-Support/work-orders")]
    public void Should_RecognizeTestSupportPaths(string path)
    {
        TestSupportMiddleware.IsTestSupportPath(path).ShouldBeTrue();
    }

    [TestCase("/api/echo")]
    [TestCase("/api/test-supportive")]
    [TestCase(null)]
    [TestCase("")]
    public void Should_IgnoreNonTestSupportPaths(string? path)
    {
        TestSupportMiddleware.IsTestSupportPath(path).ShouldBeFalse();
    }
}
