using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.BuildGates;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

/// <summary>Packs duration shards the same way PR-environment parallel lanes exclude the serial lane.</summary>
internal static class SerialLaneDurationPack
{
    /// <summary>Class names packed after removing <c>acceptance-serial-lane.json</c> fixtures.</summary>
    public static IReadOnlySet<string> PackedClassNames(RepositoryCheckout checkout, int shardCount = 12)
    {
        using var serialDoc = JsonDocument.Parse(checkout.ReadText("src/AcceptanceTests/acceptance-serial-lane.json"));
        var serialNames = serialDoc.RootElement.GetProperty("classes").EnumerateArray()
            .Select(element => element.GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        using var durationsDoc = JsonDocument.Parse(checkout.ReadText("src/AcceptanceTests/acceptance-durations.json"));
        var classes = durationsDoc.RootElement.GetProperty("classes").EnumerateArray()
            .Select(element => new DurationClass(
                element.GetProperty("name").GetString()!,
                element.GetProperty("seconds").GetDouble()))
            .Where(item => !serialNames.Contains(item.Name))
            .ToArray();

        return DurationShardPacker.Pack(classes, shardCount)
            .SelectMany(bucket => bucket.Classes)
            .Select(item => item.Name)
            .ToHashSet(StringComparer.Ordinal);
    }
}
