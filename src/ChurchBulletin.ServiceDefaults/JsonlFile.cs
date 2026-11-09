namespace ChurchBulletin.ServiceDefaults;

/// <summary>
/// One append-only JSON-lines file that rolls to a numbered sequence file when it reaches its size cap.
/// </summary>
internal sealed class JsonlFile(string path, long maxBytes, Action onRoll) : IDisposable
{
    private readonly Lock _gate = new();
    private StreamWriter? _writer = Open(path);

    public void WriteLine(string line)
    {
        lock (_gate)
        {
            if (_writer is null) return;

            RollIfFull();
            _writer?.WriteLine(line);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    private void RollIfFull()
    {
        if (_writer!.BaseStream.Length < maxBytes) return;

        _writer.Dispose();
        _writer = null;
        TelemetryFileMaintenance.TryMoveToNextSequence(path);
        onRoll();
        _writer = Open(path);
    }

    private static StreamWriter Open(string filePath) => new(filePath, append: true) { AutoFlush = true };
}
