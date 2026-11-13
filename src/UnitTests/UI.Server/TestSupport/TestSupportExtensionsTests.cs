using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class TestSupportExtensionsTests
{
    [Test]
    public void ShouldRegisterDataService_WhenAddTestSupportCalled()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TestSupport:Enabled"] = "false"
        }).Build();
        services.AddTestSupport(config);
        services.Any(d => d.ServiceType.Name == "TestSupportDataService").ShouldBeTrue();
    }
}
