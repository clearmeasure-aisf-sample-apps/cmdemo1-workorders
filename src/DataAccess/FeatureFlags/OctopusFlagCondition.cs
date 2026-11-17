using OpenFeature.Error;
using OpenFeature.Model;

namespace ClearMeasure.Bootcamp.DataAccess.FeatureFlags;

/// <summary>
/// One client-side condition of an Octopus feature toggle rule in the v4 evaluations response. <see cref="Type"/> says
/// how the other properties are read. As in Octopus.OpenFeature, a type this provider does not know never matches, and a
/// malformed condition throws <see cref="ParseErrorException"/>.
/// </summary>
internal sealed record OctopusFlagCondition(string? Type, string? Key, string?[]? Values, int? Percentage)
{
    private const string ContextAttributeIsOneOf = "context-attribute-is-one-of";
    private const string ContextAttributeIsNotOneOf = "context-attribute-is-not-one-of";
    private const string PercentageByContext = "percentage-by-context";

    /// <summary>Whether the condition holds for <paramref name="context"/>.</summary>
    public bool Matches(EvaluationContext? context) => Type switch
    {
        ContextAttributeIsOneOf => AttributeIsOneOfValues(context),
        ContextAttributeIsNotOneOf => !AttributeIsOneOfValues(context),
        PercentageByContext => IncludedByPercentage(context),
        null => throw new ParseErrorException("A condition is missing a type."),
        _ => false
    };

    private bool AttributeIsOneOfValues(EvaluationContext? context)
    {
        var (key, values) = AttributeAndValues();
        return context is not null && context.AsDictionary().Any(entry =>
            entry.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
            && entry.Value.AsString is { } text
            && values.Contains(text, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>The attribute and accepted values of an is-one-of condition, after checking none is missing.</summary>
    private (string Key, string?[] Values) AttributeAndValues()
    {
        if (Key is null)
        {
            throw new ParseErrorException("A condition is missing a key.");
        }

        if (Values is not { Length: > 0 })
        {
            throw new ParseErrorException("A condition is missing values.");
        }

        return Values.Contains(null)
            ? throw new ParseErrorException("A condition is missing a value.")
            : (Key, Values);
    }

    private bool IncludedByPercentage(EvaluationContext? context)
    {
        var percentage = Percentage ?? throw new ParseErrorException("A condition is missing a percentage value.");
        if (percentage is < 0 or > 100)
        {
            throw new ParseErrorException($"A condition has a percentage of {percentage}.");
        }

        if (string.IsNullOrEmpty(context?.TargetingKey))
        {
            return percentage >= 100;
        }

        // Octopus places a targeting key in a rollout by a Murmur3 hash of the evaluation key and targeting key. This app
        // never sends a targeting key, so the interim provider reports the case instead of hashing it differently.
        throw new GeneralException("Percentage rollouts by targeting key need the Octopus.OpenFeature provider.");
    }
}
