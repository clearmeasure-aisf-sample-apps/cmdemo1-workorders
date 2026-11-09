namespace ClearMeasure.Bootcamp.UI.Shared.Models;

/// <summary>
/// One section of the reports CSV: its column heading, the category written on each row, and the keys in the order
/// they are listed. Keys with no count are left out.
/// </summary>
public sealed record CountSection(string Heading, string Category, string[] Order)
{
    public IEnumerable<KeyValuePair<string, int>> Listed(IReadOnlyDictionary<string, int> counts) =>
        Order.Where(counts.ContainsKey).Select(key => KeyValuePair.Create(key, counts[key]));
}
