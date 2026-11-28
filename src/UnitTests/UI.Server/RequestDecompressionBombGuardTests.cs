using System.IO.Compression;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RequestDecompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestDecompressionBombGuardTests
{
    [Test]
    public async Task ShouldReadWholeBody_WhenDecompressedSizeIsWithinLimit()
    {
        await using var stream = new DecompressedSizeLimitStream(new MemoryStream(new byte[100]), 100);

        var read = await ReadToEndAsync(stream);

        read.ShouldBe(100);
    }

    [Test]
    public void ShouldThrow413_WhenDecompressedSizeExceedsLimit()
    {
        using var stream = new DecompressedSizeLimitStream(new MemoryStream(new byte[101]), 100);

        var exception = Should.Throw<BadHttpRequestException>(() => ReadToEndSync(stream));

        exception.StatusCode.ShouldBe(StatusCodes.Status413PayloadTooLarge);
    }

    [Test]
    public async Task ShouldThrow413_WhenReadAsyncArrayOverloadPassesLimit()
    {
        await using var stream = new DecompressedSizeLimitStream(new MemoryStream(new byte[64]), 10);
        var buffer = new byte[64];

        var exception = await Should.ThrowAsync<BadHttpRequestException>(
            () => stream.ReadAsync(buffer, 0, buffer.Length));

        exception.StatusCode.ShouldBe(StatusCodes.Status413PayloadTooLarge);
    }

    [Test]
    public void ShouldAccumulateAcrossReads_WhenSpanReadsPassLimit()
    {
        using var stream = new DecompressedSizeLimitStream(new MemoryStream(new byte[24]), 15);
        var buffer = new byte[8];

        stream.Read(buffer.AsSpan()).ShouldBe(8);
        Should.Throw<BadHttpRequestException>(() => stream.Read(buffer.AsSpan()));
    }

    [Test]
    public void ShouldRejectWritesAndSeeks_WhenStreamIsReadOnlyLimit()
    {
        using var stream = new DecompressedSizeLimitStream(new MemoryStream(), 10);

        stream.CanSeek.ShouldBeFalse();
        Should.Throw<NotSupportedException>(() => stream.Write([1], 0, 1));
        Should.Throw<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
    }

    [TestCase(0L, 1L)]
    [TestCase(-5L, 1L)]
    [TestCase(2048L, 2048L)]
    public void ShouldClampLimit_WhenConfiguredBelowOne(long configured, long expected)
    {
        RequestDecompressionBombGuard.ClampLimit(configured).ShouldBe(expected);
    }

    [Test]
    public void ShouldReturnNull_WhenRequestHasNoContentEncoding()
    {
        var guard = new RequestDecompressionBombGuard(new StubDecompressionProvider(null), Options(true, 10));

        guard.GetDecompressionStream(new DefaultHttpContext()).ShouldBeNull();
    }

    [Test]
    public void ShouldReturnInnerStream_WhenGuardDisabled()
    {
        var inner = new MemoryStream();
        var guard = new RequestDecompressionBombGuard(new StubDecompressionProvider(inner), Options(false, 10));

        guard.GetDecompressionStream(new DefaultHttpContext()).ShouldBeSameAs(inner);
    }

    [Test]
    public void ShouldWrapInnerStream_WhenGuardEnabled()
    {
        var guard = new RequestDecompressionBombGuard(new StubDecompressionProvider(new MemoryStream()), Options(true, 10));

        guard.GetDecompressionStream(new DefaultHttpContext()).ShouldBeOfType<DecompressedSizeLimitStream>();
    }

    [Test]
    public async Task ShouldReject413_WhenGzipBombExpandsPastConfiguredLimit()
    {
        await using var services = BuildServices(new Dictionary<string, string?>
        {
            ["RequestDecompressionBombGuard:MaxDecompressedBytes"] = "1024"
        });
        var context = GzipContext(new byte[1024 * 1024]);

        await using var body = services.GetRequiredService<IRequestDecompressionProvider>()
            .GetDecompressionStream(context)!;

        var exception = await Should.ThrowAsync<BadHttpRequestException>(() => ReadToEndAsync(body));
        exception.StatusCode.ShouldBe(StatusCodes.Status413PayloadTooLarge);
    }

    [Test]
    public async Task ShouldDecompressNormally_WhenGzipBodyIsWithinDefaultLimit()
    {
        await using var services = BuildServices(new Dictionary<string, string?>());
        var context = GzipContext(new byte[4096]);

        await using var body = services.GetRequiredService<IRequestDecompressionProvider>()
            .GetDecompressionStream(context)!;

        (await ReadToEndAsync(body)).ShouldBe(4096);
    }

    [Test]
    public void ShouldUseDefaults_WhenConfigurationSectionMissing()
    {
        using var services = BuildServices(new Dictionary<string, string?>());

        var options = services.GetRequiredService<IOptions<RequestDecompressionBombGuardOptions>>().Value;

        options.Enabled.ShouldBeTrue();
        options.MaxDecompressedBytes.ShouldBe(RequestDecompressionBombGuardOptions.DefaultMaxDecompressedBytes);
        services.GetRequiredService<IRequestDecompressionProvider>().ShouldBeOfType<RequestDecompressionBombGuard>();
    }

    private static ServiceProvider BuildServices(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRequestDecompressionBombGuard(configuration);
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext GzipContext(byte[] payload)
    {
        var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(payload);
        }

        compressed.Position = 0;
        var context = new DefaultHttpContext();
        context.Request.Headers.ContentEncoding = "gzip";
        context.Request.Body = compressed;
        return context;
    }

    private static IOptions<RequestDecompressionBombGuardOptions> Options(bool enabled, long maxBytes) =>
        Microsoft.Extensions.Options.Options.Create(
            new RequestDecompressionBombGuardOptions { Enabled = enabled, MaxDecompressedBytes = maxBytes });

    private static async Task<int> ReadToEndAsync(Stream stream)
    {
        var buffer = new byte[256];
        var total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            total += read;
        }

        return total;
    }

    private static void ReadToEndSync(Stream stream)
    {
        var buffer = new byte[16];
        while (stream.Read(buffer, 0, buffer.Length) > 0)
        {
        }
    }

    private sealed class StubDecompressionProvider(Stream? stream) : IRequestDecompressionProvider
    {
        public Stream? GetDecompressionStream(HttpContext context) => stream;
    }
}
