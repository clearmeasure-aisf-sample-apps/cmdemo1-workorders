using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class AutoReformatOptionsTests
{
    [Test]
    public void AddAutoReformatOptions_SetsDisabled_WhenFlatKeyIsTrue()
    {
        var configuration = ConfigurationWith(
            (AutoReformatOptions.DisableConfigurationKey, "true"));
        var services = new ServiceCollection();
        services.AddAutoReformatOptions(configuration);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<AutoReformatOptions>>().Value;

        options.Disabled.ShouldBeTrue();
        options.PollInterval.ShouldBe(TimeSpan.FromSeconds(5));
        options.ErrorRetryDelay.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Test]
    public void AddAutoReformatOptions_BindsSectionDelays()
    {
        var configuration = ConfigurationWith(
            ($"{AutoReformatOptions.SectionName}:PollIntervalSeconds", "7"),
            ($"{AutoReformatOptions.SectionName}:ErrorRetryDelaySeconds", "42"));
        var services = new ServiceCollection();
        services.AddAutoReformatOptions(configuration);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<AutoReformatOptions>>().Value;

        options.PollInterval.ShouldBe(TimeSpan.FromSeconds(7));
        options.ErrorRetryDelay.ShouldBe(TimeSpan.FromSeconds(42));
        options.Disabled.ShouldBeFalse();
    }

    private static IConfiguration ConfigurationWith(params (string Key, string Value)[] pairs) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.ToDictionary(pair => pair.Key, pair => (string?)pair.Value))
            .Build();
}
