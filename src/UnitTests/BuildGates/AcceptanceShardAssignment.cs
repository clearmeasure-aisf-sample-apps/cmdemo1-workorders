using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// One acceptance test class in <c>src/AcceptanceTests/acceptance-shards.json</c>.
/// Durations are measured seconds from a green Build, not a guess.
/// </summary>
internal sealed class AcceptanceShardClass
{
    public string Name { get; init; } = "";

    public int Shard { get; init; }

    public int Tests { get; init; }

    public double X64Seconds { get; init; }

    public double ArmSeconds { get; init; }

    public double LongestX64Seconds { get; init; }

    public double LongestArmSeconds { get; init; }
}

/// <summary>
/// The checked-in map from acceptance test class to shard. Shards 1 through
/// <see cref="ShardCount"/> are explicit; nothing is assigned by exclusion.
/// </summary>
internal sealed class AcceptanceShardAssignment
{
    public const string RelativePath = "src/AcceptanceTests/acceptance-shards.json";

    public int ShardCount { get; init; }

    public int Workers { get; init; }

    public double MaxDurationRatio { get; init; }

    public double MaxCountRatio { get; init; }

    public double DefaultNewTestSeconds { get; init; }

    public List<AcceptanceShardClass> Classes { get; init; } = [];

    /// <summary>Reads <see cref="RelativePath"/> from <paramref name="checkout"/>.</summary>
    public static AcceptanceShardAssignment Load(RepositoryCheckout checkout)
    {
        var assignment = JsonSerializer.Deserialize<AcceptanceShardAssignment>(
            checkout.ReadText(RelativePath),
            SerializerOptions());
        if (assignment is null || assignment.Classes.Count == 0)
        {
            throw new InvalidOperationException($"{RelativePath} has no classes.");
        }

        return assignment;
    }

    private static JsonSerializerOptions SerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
    }
}
