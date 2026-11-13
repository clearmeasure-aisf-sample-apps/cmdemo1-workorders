using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class TestSupportSecretTests
{
    [Test]
    public void Matches_WhenHeaderEqualsSecret_ReturnsTrue()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers[TestSupportConstants.SecretHeaderName] = "abc";

        TestSupportSecret.Matches(request, "abc").ShouldBeTrue();
    }

    [Test]
    public void Matches_WhenHeaderMissing_ReturnsFalse()
    {
        var request = new DefaultHttpContext().Request;

        TestSupportSecret.Matches(request, "abc").ShouldBeFalse();
    }

    [Test]
    public void Matches_WhenSecretEmpty_ReturnsFalse()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers[TestSupportConstants.SecretHeaderName] = "abc";

        TestSupportSecret.Matches(request, "").ShouldBeFalse();
    }
}
