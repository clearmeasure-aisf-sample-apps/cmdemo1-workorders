using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Exemptions for a structure rule. Every entry needs a written reason (test seams, framework conventions).
/// </summary>
public sealed class StructureAllowList
{
    private readonly Dictionary<string, Dictionary<string, string>> _entries;

    private StructureAllowList(Dictionary<string, Dictionary<string, string>> entries)
    {
        _entries = entries;
    }

    /// <summary>
    /// Parses an allowlist document. A blank or missing reason fails the load.
    /// </summary>
    public static StructureAllowList Load(string json)
    {
        using var document = JsonDocument.Parse(json);
        var entries = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var rule in StructureRule.All)
        {
            entries[rule] = ReadRule(document.RootElement, rule);
        }

        return new StructureAllowList(entries);
    }

    /// <summary>
    /// Exempted ids for <paramref name="rule"/>.
    /// </summary>
    public IReadOnlyCollection<string> Ids(string rule)
    {
        return Entries(rule).Keys;
    }

    /// <summary>
    /// Written reason for an exempted id.
    /// </summary>
    public string Reason(string rule, string id)
    {
        return Entries(rule)[id];
    }

    private Dictionary<string, string> Entries(string rule)
    {
        if (!_entries.TryGetValue(rule, out var entries))
        {
            throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unknown structure rule.");
        }

        return entries;
    }

    private static Dictionary<string, string> ReadRule(JsonElement root, string rule)
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!root.TryGetProperty(rule, out var array))
        {
            return entries;
        }

        if (array.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"Allowlist rule '{rule}' must be an array.");
        }

        foreach (var item in array.EnumerateArray())
        {
            var id = ReadId(item, rule);
            var reason = ReadReason(item, rule, id);
            if (!entries.TryAdd(id, reason))
            {
                throw new InvalidDataException($"Allowlist rule '{rule}' lists '{id}' more than once.");
            }
        }

        return entries;
    }

    private static string ReadId(JsonElement item, string rule)
    {
        if (!item.TryGetProperty("id", out var idProperty) || idProperty.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"Allowlist rule '{rule}' has an entry without an id.");
        }

        var id = idProperty.GetString();
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidDataException($"Allowlist rule '{rule}' has a blank id.");
        }

        return id;
    }

    private static string ReadReason(JsonElement item, string rule, string id)
    {
        if (!item.TryGetProperty("reason", out var reasonProperty) || reasonProperty.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"Allowlist rule '{rule}' entry '{id}' is missing a reason.");
        }

        var reason = reasonProperty.GetString()?.Trim() ?? "";
        if (reason.Length == 0)
        {
            throw new InvalidDataException($"Allowlist rule '{rule}' entry '{id}' is missing a reason.");
        }

        return reason;
    }
}
