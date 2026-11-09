using System.Text.Json;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
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
        var path = Path.Combine(FindRepoRoot(), "src", "UI", "Server", fileName);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var section = document.RootElement.GetProperty("FeatureFlags");
        var flags = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var property in section.EnumerateObject())
        {
            flags[property.Name] = property.Value.GetBoolean();
        }

        return flags;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "ChurchBulletin.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root from test directory.");
    }
}
