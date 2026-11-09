using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Grandfathered findings. The committed max may only stay or fall; a new id fails the gate.
/// </summary>
public sealed class StructureBaseline
{
    private readonly Dictionary<string, BaselineRule> _rules;

    private StructureBaseline(Dictionary<string, BaselineRule> rules)
    {
        _rules = rules;
    }

    /// <summary>
    /// Parses a baseline document. Each rule needs <c>max</c> and <c>ids</c>, and <c>max</c> must cover <c>ids</c>.
    /// </summary>
    public static StructureBaseline Load(string json)
    {
        using var document = JsonDocument.Parse(json);
        var rules = new Dictionary<string, BaselineRule>(StringComparer.Ordinal);
        foreach (var rule in StructureRule.All)
        {
            rules[rule] = ReadRule(document.RootElement, rule);
        }

        return new StructureBaseline(rules);
    }

    /// <summary>Ceiling for <paramref name="rule"/>. The live count must not exceed it.</summary>
    public int Max(string rule)
    {
        return Rule(rule).Max;
    }

    /// <summary>Grandfathered ids for <paramref name="rule"/>.</summary>
    public IReadOnlySet<string> Ids(string rule)
    {
        return Rule(rule).Ids;
    }

    private BaselineRule Rule(string rule)
    {
        if (!_rules.TryGetValue(rule, out var baselineRule))
        {
            throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unknown structure rule.");
        }

        return baselineRule;
    }

    private static BaselineRule ReadRule(JsonElement root, string rule)
    {
        if (!root.TryGetProperty(rule, out var element) || element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"Baseline is missing object '{rule}'.");
        }

        if (!element.TryGetProperty("max", out var maxProperty) || maxProperty.ValueKind != JsonValueKind.Number)
        {
            throw new InvalidDataException($"Baseline rule '{rule}' is missing numeric max.");
        }

        var ids = ReadIds(element, rule);
        var max = maxProperty.GetInt32();
        if (max < ids.Count)
        {
            throw new InvalidDataException(
                $"Baseline rule '{rule}' max {max} is below the grandfathered list ({ids.Count}).");
        }

        return new BaselineRule(max, ids);
    }

    private static HashSet<string> ReadIds(JsonElement element, string rule)
    {
        if (!element.TryGetProperty("ids", out var idsProperty) || idsProperty.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"Baseline rule '{rule}' is missing ids.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in idsProperty.EnumerateArray())
        {
            var id = item.GetString();
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
            {
                throw new InvalidDataException($"Baseline rule '{rule}' has a blank or duplicate id.");
            }
        }

        return ids;
    }

    private sealed record BaselineRule(int Max, HashSet<string> Ids);
}
