namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Read-only stream that counts the bytes read from a decompression stream and throws
/// <see cref="BadHttpRequestException"/> (413) once the count passes the limit.
/// </summary>
internal sealed class DecompressedSizeLimitStream(Stream inner, long limit) : Stream
{
    private long _bytesRead;

    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => Count(inner.Read(buffer, offset, count));

    public override int Read(Span<byte> buffer) => Count(inner.Read(buffer));

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        Count(await inner.ReadAsync(buffer, cancellationToken));

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            inner.Dispose();
        }

        base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync() => inner.DisposeAsync();

    private int Count(int read)
    {
        _bytesRead += read;
        if (_bytesRead > limit)
        {
            throw new BadHttpRequestException(
                $"Decompressed request body is larger than the {limit}-byte limit.",
                StatusCodes.Status413PayloadTooLarge);
        }

        return read;
    }
}
