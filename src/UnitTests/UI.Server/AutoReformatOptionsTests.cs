using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class AutoReformatOptionsTests
{
    [Test]
    public void Constructor_SetsDisabled_WhenFlatKeyIsTrue()
    {
        var configuration = ConfigurationWith(
            (AutoReformatOptions.DisableConfigurationKey, "true"));

        var options = new AutoReformatOptions(configuration);

        options.Disabled.ShouldBeTrue();
        options.PollInterval.ShouldBe(TimeSpan.FromSeconds(5));
        options.ErrorRetryDelay.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Test]
    public void Constructor_BindsSectionDelays()
    {
        var configuration = ConfigurationWith(
            ($"{AutoReformatOptions.SectionName}:PollIntervalSeconds", "7"),
            ($"{AutoReformatOptions.SectionName}:ErrorRetryDelaySeconds", "42"));

        var options = new AutoReformatOptions(configuration);

        options.PollInterval.ShouldBe(TimeSpan.FromSeconds(7));
        options.ErrorRetryDelay.ShouldBe(TimeSpan.FromSeconds(42));
        options.Disabled.ShouldBeFalse();
    }

    private static IConfiguration ConfigurationWith(params (string Key, string Value)[] pairs) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.ToDictionary(pair => pair.Key, pair => (string?)pair.Value))
            .Build();
}
