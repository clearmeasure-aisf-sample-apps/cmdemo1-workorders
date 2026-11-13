namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

/// <summary>
/// Greedy duration balancer: assigns each class to the shard with the smallest running total.
/// Used by the 12-runner PR-environment plan without changing the CI 6-shard map.
/// </summary>
public static class DurationShardPacker
{
    /// <summary>
    /// Packs <paramref name="classes"/> into <paramref name="shardCount"/> shards by descending duration.
    /// </summary>
    public static IReadOnlyList<DurationShardBucket> Pack(
        IReadOnlyList<DurationClass> classes,
        int shardCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(shardCount, 1);
        var buckets = Enumerable.Range(1, shardCount)
            .Select(shard => new DurationShardBucket(shard))
            .ToArray();

        foreach (var item in classes.OrderByDescending(c => c.Seconds).ThenBy(c => c.Name, StringComparer.Ordinal))
        {
            var lightest = buckets.OrderBy(b => b.TotalSeconds).ThenBy(b => b.Shard).First();
            lightest.Add(item);
        }

        return buckets;
    }
}
