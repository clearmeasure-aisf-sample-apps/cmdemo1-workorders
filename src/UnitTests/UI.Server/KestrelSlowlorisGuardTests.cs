using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class KestrelSlowlorisGuardTests
{
    [Test]
    public void ShouldApplySafeDefaults_WhenSectionIsAbsent()
    {
        var limits = new KestrelServerOptions().Limits;

        KestrelSlowlorisGuard.Apply(limits, Configuration(new Dictionary<string, string?>()));

        limits.RequestHeadersTimeout.ShouldBe(TimeSpan.FromSeconds(15));
        limits.KeepAliveTimeout.ShouldBe(TimeSpan.FromSeconds(130));
        limits.MinRequestBodyDataRate!.BytesPerSecond.ShouldBe(240);
        limits.MinRequestBodyDataRate.GracePeriod.ShouldBe(TimeSpan.FromSeconds(5));
        limits.MinResponseDataRate!.BytesPerSecond.ShouldBe(240);
        limits.MinResponseDataRate.GracePeriod.ShouldBe(TimeSpan.FromSeconds(5));
        limits.MaxConcurrentConnections.ShouldBe(2000);
        limits.MaxConcurrentUpgradedConnections.ShouldBe(1000);
    }

    [Test]
    public void ShouldBindConfiguredValues_WhenSectionIsPresent()
    {
        var limits = new KestrelServerOptions().Limits;

        KestrelSlowlorisGuard.Apply(limits, Configuration(new Dictionary<string, string?>
        {
            ["KestrelSlowlorisGuard:RequestHeadersTimeout"] = "00:00:20",
            ["KestrelSlowlorisGuard:KeepAliveTimeout"] = "00:01:00",
            ["KestrelSlowlorisGuard:MinRequestBodyBytesPerSecond"] = "500",
            ["KestrelSlowlorisGuard:MinResponseBytesPerSecond"] = "600",
            ["KestrelSlowlorisGuard:MinDataRateGracePeriod"] = "00:00:10",
            ["KestrelSlowlorisGuard:MaxConcurrentConnections"] = "300",
            ["KestrelSlowlorisGuard:MaxConcurrentUpgradedConnections"] = "50",
        }));

        limits.RequestHeadersTimeout.ShouldBe(TimeSpan.FromSeconds(20));
        limits.KeepAliveTimeout.ShouldBe(TimeSpan.FromMinutes(1));
        limits.MinRequestBodyDataRate!.BytesPerSecond.ShouldBe(500);
        limits.MinResponseDataRate!.BytesPerSecond.ShouldBe(600);
        limits.MinResponseDataRate.GracePeriod.ShouldBe(TimeSpan.FromSeconds(10));
        limits.MaxConcurrentConnections.ShouldBe(300);
        limits.MaxConcurrentUpgradedConnections.ShouldBe(50);
    }

    [Test]
    public void ShouldClampToMinimums_WhenValuesAreNonPositive()
    {
        var limits = new KestrelServerOptions().Limits;

        KestrelSlowlorisGuard.Apply(limits, new KestrelSlowlorisGuardOptions
        {
            RequestHeadersTimeout = TimeSpan.Zero,
            KeepAliveTimeout = TimeSpan.FromSeconds(-5),
            MinRequestBodyBytesPerSecond = 0,
            MinResponseBytesPerSecond = -10,
            MinDataRateGracePeriod = TimeSpan.Zero,
            MaxConcurrentConnections = 0,
            MaxConcurrentUpgradedConnections = -1,
        });

        limits.RequestHeadersTimeout.ShouldBe(TimeSpan.FromSeconds(1));
        limits.KeepAliveTimeout.ShouldBe(TimeSpan.FromSeconds(1));
        limits.MinRequestBodyDataRate!.BytesPerSecond.ShouldBe(1);
        limits.MinResponseDataRate!.BytesPerSecond.ShouldBe(1);
        limits.MinResponseDataRate.GracePeriod.ShouldBe(TimeSpan.FromSeconds(2));
        limits.MaxConcurrentConnections.ShouldBe(1);
        limits.MaxConcurrentUpgradedConnections.ShouldBe(1);
    }

    [Test]
    public void ShouldClampToMaximums_WhenValuesAreAbsurd()
    {
        var limits = new KestrelServerOptions().Limits;

        KestrelSlowlorisGuard.Apply(limits, new KestrelSlowlorisGuardOptions
        {
            RequestHeadersTimeout = TimeSpan.FromDays(10),
            KeepAliveTimeout = TimeSpan.MaxValue,
            MinRequestBodyBytesPerSecond = double.PositiveInfinity,
            MinResponseBytesPerSecond = double.NaN,
            MinDataRateGracePeriod = TimeSpan.FromHours(1),
            MaxConcurrentConnections = long.MaxValue,
            MaxConcurrentUpgradedConnections = long.MaxValue,
        });

        limits.RequestHeadersTimeout.ShouldBe(TimeSpan.FromMinutes(10));
        limits.KeepAliveTimeout.ShouldBe(TimeSpan.FromMinutes(10));
        limits.MinRequestBodyDataRate!.BytesPerSecond.ShouldBe(1024 * 1024);
        limits.MinResponseDataRate!.BytesPerSecond.ShouldBe(240);
        limits.MinResponseDataRate.GracePeriod.ShouldBe(TimeSpan.FromSeconds(60));
        limits.MaxConcurrentConnections.ShouldBe(1_000_000);
        limits.MaxConcurrentUpgradedConnections.ShouldBe(1_000_000);
    }

    [Test]
    public void ShouldMatchAppSettingsDefaults_WhenOptionsAreConstructed()
    {
        var fromAppSettings = new KestrelServerOptions().Limits;
        var fromDefaults = new KestrelServerOptions().Limits;
        var appSettings = new ConfigurationBuilder()
            .AddJsonFile(RepositoryCheckout.Current.PathOf("src/UI/Server/appsettings.json"), optional: false)
            .Build();

        KestrelSlowlorisGuard.Apply(fromAppSettings, appSettings);
        KestrelSlowlorisGuard.Apply(fromDefaults, new KestrelSlowlorisGuardOptions());

        fromAppSettings.RequestHeadersTimeout.ShouldBe(fromDefaults.RequestHeadersTimeout);
        fromAppSettings.KeepAliveTimeout.ShouldBe(fromDefaults.KeepAliveTimeout);
        fromAppSettings.MaxConcurrentConnections.ShouldBe(fromDefaults.MaxConcurrentConnections);
        fromAppSettings.MaxConcurrentUpgradedConnections.ShouldBe(fromDefaults.MaxConcurrentUpgradedConnections);
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
