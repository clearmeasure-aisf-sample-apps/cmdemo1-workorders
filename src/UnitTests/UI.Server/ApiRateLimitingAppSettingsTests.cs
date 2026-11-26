using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>
/// The <c>ApiRateLimiting</c> section of <c>appsettings.json</c> is kept as documentation of the shipped limiter: it must
/// restate the <see cref="ApiRateLimitingOptions"/> defaults exactly, and Development may only turn the limiter off.
/// </summary>
[TestFixture]
public class ApiRateLimitingAppSettingsTests
{
    [Test]
    public void Should_EqualCodeDefaults_When_AppsettingsSectionIsBound()
    {
        var bound = Bind("appsettings.json");

        ShouldMatchDefaultsExceptEnabled(bound);
        bound.Enabled.ShouldBe(new ApiRateLimitingOptions().Enabled);
    }

    [Test]
    public void Should_DisableOnlyEnabled_When_DevelopmentAppsettingsAreLayered()
    {
        var bound = Bind("appsettings.json", "appsettings.Development.json");

        ShouldMatchDefaultsExceptEnabled(bound);
        bound.Enabled.ShouldBeFalse();
    }

    private static void ShouldMatchDefaultsExceptEnabled(ApiRateLimitingOptions bound)
    {
        var defaults = new ApiRateLimitingOptions();
        bound.PermitLimit.ShouldBe(defaults.PermitLimit);
        bound.WindowSeconds.ShouldBe(defaults.WindowSeconds);
        bound.SegmentsPerWindow.ShouldBe(defaults.SegmentsPerWindow);
        bound.QueueLimit.ShouldBe(defaults.QueueLimit);
        bound.ApiKeyHeaderName.ShouldBe(defaults.ApiKeyHeaderName);
    }

    private static ApiRateLimitingOptions Bind(params string[] fileNames)
    {
        var builder = new ConfigurationBuilder();
        foreach (var fileName in fileNames)
        {
            builder.AddJsonFile(RepositoryCheckout.Current.PathOf($"src/UI/Server/{fileName}"), optional: false);
        }

        var section = builder.Build().GetSection(ApiRateLimitingOptions.SectionName);
        section.Exists().ShouldBeTrue();
        // Bind onto deliberately non-default values so a key missing from the JSON cannot pass by inheriting the default.
        var bound = new ApiRateLimitingOptions
        {
            Enabled = false, PermitLimit = -1, WindowSeconds = -1, SegmentsPerWindow = -1, QueueLimit = -1,
            ApiKeyHeaderName = string.Empty,
        };
        section.Bind(bound);
        return bound;
    }
}
