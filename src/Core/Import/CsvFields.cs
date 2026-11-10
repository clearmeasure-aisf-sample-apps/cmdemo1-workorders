using System.Text;

namespace ClearMeasure.Bootcamp.Core.Import;

/// <summary>
/// Splits one CSV record into its fields: comma separated; a field in double quotes may hold commas, line breaks and
/// doubled quotes (<c>""</c> is one quote).
/// </summary>
public static class CsvFields
{
    public static List<string> Split(string record)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var i = 0;
        while (i < record.Length)
        {
            i = record[i] switch
            {
                '"' => AppendQuoted(record, i + 1, field),
                ',' => EndField(fields, field, i),
                _ => AppendCharacter(record, i, field)
            };
        }

        fields.Add(field.ToString());
        return fields;
    }

    /// <summary>Whether a doubled quote (an escaped quote inside a quoted field) starts at <paramref name="index"/>.</summary>
    public static bool IsEscapedQuote(string text, int index) =>
        text[index] == '"' && index + 1 < text.Length && text[index + 1] == '"';

    private static int EndField(List<string> fields, StringBuilder field, int index)
    {
        fields.Add(field.ToString());
        field.Clear();
        return index + 1;
    }

    private static int AppendCharacter(string record, int index, StringBuilder field)
    {
        field.Append(record[index]);
        return index + 1;
    }

    /// <summary>Appends the quoted field starting after its opening quote; returns the index after its closing quote.</summary>
    private static int AppendQuoted(string record, int start, StringBuilder field)
    {
        var i = start;
        while (i < record.Length)
        {
            if (IsEscapedQuote(record, i))
            {
                field.Append('"');
                i += 2;
            }
            else if (record[i] == '"')
            {
                return i + 1;
            }
            else
            {
                field.Append(record[i++]);
            }
        }

        return i;
    }
}
