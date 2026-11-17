using OpenFeature.Error;
using OpenFeature.Model;

namespace ClearMeasure.Bootcamp.DataAccess.FeatureFlags;

/// <summary>A named client-side rule of an Octopus feature toggle: it matches when every condition matches.</summary>
internal sealed record OctopusFlagRule(string? Name, OctopusFlagCondition?[]? Conditions)
{
    /// <summary>Whether every condition holds for <paramref name="context"/>, read in order until one does not.</summary>
    public bool Matches(EvaluationContext? context)
    {
        if (Name is null)
        {
            throw new ParseErrorException("A rule has no name.");
        }

        if (Conditions is not { Length: > 0 })
        {
            throw new ParseErrorException($"Rule '{Name}' has no conditions.");
        }

        foreach (var condition in Conditions)
        {
            if (condition is null)
            {
                throw new ParseErrorException($"Rule '{Name}' has a missing condition.");
            }

            if (!condition.Matches(context))
            {
                return false;
            }
        }

        return true;
    }
}
