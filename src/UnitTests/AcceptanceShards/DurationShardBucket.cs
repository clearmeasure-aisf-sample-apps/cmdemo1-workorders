namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

/// <summary>One duration shard and the classes assigned to it.</summary>
public sealed class DurationShardBucket(int shard)
{
    private readonly List<DurationClass> _classes = [];

    /// <summary>1-based shard number.</summary>
    public int Shard { get; } = shard;

    /// <summary>Classes on this shard.</summary>
    public IReadOnlyList<DurationClass> Classes => _classes;

    /// <summary>Sum of class durations.</summary>
    public double TotalSeconds { get; private set; }

    /// <summary>Adds a class to this shard.</summary>
    public void Add(DurationClass item)
    {
        _classes.Add(item);
        TotalSeconds += item.Seconds;
    }
}
