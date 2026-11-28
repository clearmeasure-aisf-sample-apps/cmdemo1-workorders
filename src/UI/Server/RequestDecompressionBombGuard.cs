using Microsoft.AspNetCore.RequestDecompression;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Decorates the framework <see cref="IRequestDecompressionProvider"/> so every decompression stream it hands to
/// <c>UseRequestDecompression</c> is capped at <see cref="RequestDecompressionBombGuardOptions.MaxDecompressedBytes"/>.
/// A small compressed body that expands past the cap fails with 413 instead of filling memory.
/// </summary>
internal sealed class RequestDecompressionBombGuard(
    IRequestDecompressionProvider inner,
    IOptions<RequestDecompressionBombGuardOptions> options) : IRequestDecompressionProvider
{
    /// <inheritdoc />
    public Stream? GetDecompressionStream(HttpContext context)
    {
        var decompressed = inner.GetDecompressionStream(context);
        var settings = options.Value;
        if (decompressed is null || !settings.Enabled)
        {
            return decompressed;
        }

        return new DecompressedSizeLimitStream(decompressed, ClampLimit(settings.MaxDecompressedBytes));
    }

    internal static long ClampLimit(long maxDecompressedBytes) =>
        maxDecompressedBytes < 1 ? 1 : maxDecompressedBytes;
}
