using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Reads the production CRAP gate threshold from the single authoritative config file.
/// </summary>
public static class CrapGateThreshold
{
    /// <summary>
    /// Relative path from the repository root to the gate threshold config.
    /// </summary>
    public const string RelativeConfigPath =
        "scripts/crap/crap-gate-threshold.json";

    /// <summary>
    /// Reads <c>productionThreshold</c> from <see cref="RelativeConfigPath"/>.
    /// </summary>
    public static int ReadProductionThreshold()
    {
        using var doc = JsonDocument.Parse(RepositoryCheckout.Current.ReadText(RelativeConfigPath));
        if (!doc.RootElement.TryGetProperty("productionThreshold", out var property)
            || property.ValueKind != JsonValueKind.Number)
        {
            throw new InvalidOperationException(
                $"{RelativeConfigPath} must contain a numeric productionThreshold property.");
        }

        return property.GetInt32();
    }

}
