using System.Text.Json;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

/// <summary>
/// Locks the shipped defaults: the image file leaves the gates off; Development (the acceptance host, matching platform tdd) turns them on.
/// </summary>
[TestFixture]
public class FeatureFlagAppSettingsTests
{
    [Test]
    public void Appsettings_Should_LeaveBuildingAndVendorOff_When_ImageDefaultRead()
    {
        var flags = ReadFlags("appsettings.json");

        flags[FeatureFlagNames.BuildingManagement].ShouldBeFalse();
        flags[FeatureFlagNames.VendorManagement].ShouldBeFalse();
    }

    [Test]
    public void DevelopmentAppsettings_Should_EnableBuildingAndVendor_When_TddHostRead()
    {
        var flags = ReadFlags("appsettings.Development.json");

        flags[FeatureFlagNames.BuildingManagement].ShouldBeTrue();
        flags[FeatureFlagNames.VendorManagement].ShouldBeTrue();
    }

    private static Dictionary<string, bool> ReadFlags(string fileName)
    {
        using var document = JsonDocument.Parse(RepositoryCheckout.Current.ReadText($"src/UI/Server/{fileName}"));
        var section = document.RootElement.GetProperty("FeatureFlags");
        var flags = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var property in section.EnumerateObject())
        {
            flags[property.Name] = property.Value.GetBoolean();
        }

        return flags;
    }

}
