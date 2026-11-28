namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Configuration for <see cref="RequestDecompressionBombGuard"/>: the most bytes a compressed request body may expand to.
/// </summary>
public sealed class RequestDecompressionBombGuardOptions
{
    /// <summary>Configuration section bound to these options.</summary>
    public const string SectionName = "RequestDecompressionBombGuard";

    /// <summary>Default decompressed-size cap: 30 MiB, the same as Kestrel's default request body limit.</summary>
    public const long DefaultMaxDecompressedBytes = 30L * 1024 * 1024;

    /// <summary>When false, decompression streams are returned without a size cap.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// The most decompressed bytes one request body may produce. Reading past it fails the request with 413.
    /// Values below 1 are treated as 1.
    /// </summary>
    public long MaxDecompressedBytes { get; init; } = DefaultMaxDecompressedBytes;
}
