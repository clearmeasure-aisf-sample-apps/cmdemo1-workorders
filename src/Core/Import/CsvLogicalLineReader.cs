using System.Text;

namespace ClearMeasure.Bootcamp.Core.Import;

/// <summary>
/// Reads one CSV record at a time: a physical line, plus the following lines while a quoted field is still open
/// (a field may contain line breaks). Counts physical lines read.
/// </summary>
public sealed class CsvLogicalLineReader(TextReader reader)
{
    /// <summary>The number of the last physical line read (1-based).</summary>
    public int LineNumber { get; private set; }

    /// <summary>The next record, its lines joined with <c>\n</c>, or <c>null</c> at the end of the text.</summary>
    public string? ReadRecord(CancellationToken cancellationToken = default)
    {
        var first = ReadPhysicalLine();
        if (first == null)
        {
            return null;
        }

        var record = new StringBuilder(first);
        while (HasUnclosedQuote(record.ToString()) && ReadContinuation(cancellationToken) is { } next)
        {
            record.Append('\n').Append(next);
        }

        return record.ToString();
    }

    private string? ReadContinuation(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ReadPhysicalLine();
    }

    private string? ReadPhysicalLine()
    {
        var line = reader.ReadLine();
        if (line != null)
        {
            LineNumber++;
        }

        return line;
    }

    private static bool HasUnclosedQuote(string text)
    {
        var quotes = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (CsvFields.IsEscapedQuote(text, i))
            {
                i++;
            }
            else if (text[i] == '"')
            {
                quotes++;
            }
        }

        return quotes % 2 != 0;
    }
}
