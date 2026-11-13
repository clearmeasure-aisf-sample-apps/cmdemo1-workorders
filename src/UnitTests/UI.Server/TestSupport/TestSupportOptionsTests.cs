using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class TestSupportOptionsTests
{
    [Test]
    public void ShouldDefaultDisabled_WhenConstructed()
    {
        var options = new TestSupportOptions(enabled: false, secret: string.Empty);
        options.Enabled.ShouldBeFalse();
        options.Secret.ShouldBeEmpty();
        TestSupportOptions.SectionName.ShouldBe("TestSupport");
    }

    [Test]
    public void ShouldReadSection_WhenFromConfigurationCalled()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TestSupport:Enabled"] = "true",
            ["TestSupport:Secret"] = "s3cret"
        }).Build();

        var options = TestSupportOptions.FromConfiguration(config);

        options.Enabled.ShouldBeTrue();
        options.Secret.ShouldBe("s3cret");
    }
}
