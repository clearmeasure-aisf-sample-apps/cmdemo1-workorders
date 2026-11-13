using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

[TestFixture]
public class SerialLaneCatalogTests
{
    public static IEnumerable<string> SerialLaneClassNames()
    {
        using var doc = JsonDocument.Parse(
            RepositoryCheckout.Current.ReadText("src/AcceptanceTests/acceptance-serial-lane.json"));
        foreach (var element in doc.RootElement.GetProperty("classes").EnumerateArray())
        {
            yield return element.GetString()!;
        }
    }

    [TestCaseSource(nameof(SerialLaneClassNames))]
    public void SerialLaneJson_ListsKnownAcceptanceClass(string name)
    {
        var known = AcceptanceTestCatalog.Read(RepositoryCheckout.Current)
            .Select(item => item.Name)
            .ToHashSet(StringComparer.Ordinal);

        known.ShouldContain(name);
    }

    [Test]
    public void SerialLaneJson_Category_IsSerialLane()
    {
        using var doc = JsonDocument.Parse(
            RepositoryCheckout.Current.ReadText("src/AcceptanceTests/acceptance-serial-lane.json"));
        doc.RootElement.GetProperty("category").GetString().ShouldBe("SerialLane");
        doc.RootElement.GetProperty("classes").GetArrayLength().ShouldBeGreaterThan(0);
    }

    [TestCaseSource(nameof(SerialLaneClassNames))]
    public void DurationPack_WhenExcludingSerialLane_OmitsClass(string serialName)
    {
        var packed = SerialLaneDurationPack.PackedClassNames(RepositoryCheckout.Current);
        packed.ShouldNotContain(serialName);
    }

    [Test]
    public void InvokeScript_WhenRead_RunsSerialOnlyAfterParallelLaneContract()
    {
        var script = RepositoryCheckout.Current.ReadText(
            "scripts/acceptance/Invoke-PrEnvironmentAcceptanceLane.ps1");

        script.ShouldContain("ValidateSet(\"Parallel\", \"Serial\")");
        script.ShouldContain("ExcludeSerialLane");
        script.ShouldContain("Get-SerialLaneFilter.ps1");
        script.ShouldContain("StartLocalServer");
        script.ShouldContain("false");
        script.ShouldContain("Workers 1");
        script.ShouldContain("never beside");
    }
}
