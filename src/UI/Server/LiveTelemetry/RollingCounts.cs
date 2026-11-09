namespace ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;

/// <summary>
/// Counts per second over a rolling window: one bucket per second of the window, reused when its second comes
/// round again. Not thread-safe on its own (the owner holds the lock); counting allocates nothing.
/// </summary>
internal sealed class RollingCounts
{
    private readonly int _windowSeconds;
    private readonly long[] _bucketSecond;
    private readonly int[] _counts;

    public RollingCounts(int windowSeconds)
    {
        _windowSeconds = windowSeconds;
        _bucketSecond = new long[windowSeconds];
        _counts = new int[windowSeconds * Counter.Count];
        Array.Fill(_bucketSecond, long.MinValue);
    }

    public void Increment(long nowMs, int counter)
    {
        var second = nowMs / 1000;
        var bucket = (int)(second % _windowSeconds);
        if (_bucketSecond[bucket] != second)
        {
            _bucketSecond[bucket] = second;
            Array.Clear(_counts, bucket * Counter.Count, Counter.Count);
        }

        _counts[bucket * Counter.Count + counter]++;
    }

    /// <summary>Each counter summed over the buckets of the window that ends at <paramref name="nowSecond"/>.</summary>
    public CounterTotals Totals(long nowSecond)
    {
        var totals = new int[Counter.Count];
        for (var bucket = 0; bucket < _windowSeconds; bucket++)
        {
            var second = _bucketSecond[bucket];
            if (second > nowSecond - _windowSeconds && second <= nowSecond)
            {
                AddBucket(totals, bucket);
            }
        }

        return new CounterTotals(totals);
    }

    private void AddBucket(int[] totals, int bucket)
    {
        for (var counter = 0; counter < Counter.Count; counter++)
        {
            totals[counter] += _counts[bucket * Counter.Count + counter];
        }
    }

    /// <summary>The counters, by position in a bucket.</summary>
    public static class Counter
    {
        public const int FrontDoorTraffic = 0;
        public const int DirectTraffic = 1;
        public const int TrafficErrors = 2;
        public const int Probes = 3;
        public const int FrontDoorProbes = 4;
        public const int SqlCommands = 5;
        public const int HttpClientCalls = 6;
        public const int UnhandledExceptions = 7;
        public const int SqlCommandsDuringRequests = 8;
        public const int Count = 9;
    }
}
