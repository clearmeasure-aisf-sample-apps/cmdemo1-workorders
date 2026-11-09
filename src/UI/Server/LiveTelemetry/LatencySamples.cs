namespace ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;

/// <summary>
/// Ring buffer of (time, milliseconds) samples; not thread-safe on its own (the owner holds the lock).
/// </summary>
internal sealed class LatencySamples(int capacity)
{
    private readonly long[] _times = new long[capacity];
    private readonly int[] _values = new int[capacity];
    private readonly int[] _scratch = new int[capacity];
    private int _next;
    private int _count;

    public void Add(long timeMs, int valueMs)
    {
        _times[_next] = timeMs;
        _values[_next] = valueMs;
        _next = (_next + 1) % _times.Length;
        _count = Math.Min(_count + 1, _times.Length);
    }

    /// <summary>Nearest-rank 95th percentile of the samples newer than <paramref name="afterMs"/>.</summary>
    public int? Percentile95(long afterMs)
    {
        var selected = 0;
        for (var i = 0; i < _count; i++)
        {
            if (_times[i] > afterMs)
            {
                _scratch[selected++] = _values[i];
            }
        }

        if (selected == 0)
        {
            return null;
        }

        var window = _scratch.AsSpan(0, selected);
        window.Sort();
        var rank = (int)Math.Ceiling(0.95 * selected);
        return window[rank - 1];
    }
}
