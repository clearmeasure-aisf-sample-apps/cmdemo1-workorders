namespace ClearMeasure.Bootcamp.Core.Import;

/// <summary>
/// The column positions named by a CSV header row, case-insensitive; the first of duplicate names wins and blank
/// names are ignored.
/// </summary>
public sealed class CsvHeader
{
    private readonly Dictionary<string, int> _columns;

    private CsvHeader(Dictionary<string, int> columns) => _columns = columns;

    public static CsvHeader Parse(string headerRecord)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var names = CsvFields.Split(headerRecord);
        for (var i = 0; i < names.Count; i++)
        {
            var name = names[i].Trim();
            if (name.Length > 0)
            {
                columns.TryAdd(name, i);
            }
        }

        return new CsvHeader(columns);
    }

    /// <summary>The first of <paramref name="required"/> the header does not name, or <c>null</c>.</summary>
    public string? FirstMissing(IEnumerable<string> required) =>
        required.FirstOrDefault(name => !_columns.ContainsKey(name));

    /// <summary>The trimmed value of the named column in <paramref name="fields"/>; <c>null</c> when absent or blank.</summary>
    public string? Cell(IReadOnlyList<string> fields, string name)
    {
        var index = _columns.GetValueOrDefault(name, -1);
        return index >= 0 && index < fields.Count && !string.IsNullOrWhiteSpace(fields[index])
            ? fields[index].Trim()
            : null;
    }
}
