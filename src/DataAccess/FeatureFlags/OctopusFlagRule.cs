using OpenFeature.Error;
using OpenFeature.Model;

namespace ClearMeasure.Bootcamp.DataAccess.FeatureFlags;

/// <summary>A named client-side rule of an Octopus feature toggle: it matches when every condition matches.</summary>
internal sealed record OctopusFlagRule(string? Name, OctopusFlagCondition?[]? Conditions)
{
    /// <summary>Whether every condition holds for <paramref name="context"/>, read in order until one does not.</summary>
    public bool Matches(EvaluationContext? context) => CheckedConditions().All(condition => condition.Matches(context));

    /// <summary>The rule's conditions, in order, each checked as it is reached so later ones are not read needlessly.</summary>
    private IEnumerable<OctopusFlagCondition> CheckedConditions()
    {
        if (Name is null)
        {
            throw new ParseErrorException("A rule has no name.");
        }

        if (Conditions is not { Length: > 0 })
        {
            throw new ParseErrorException($"Rule '{Name}' has no conditions.");
        }

        return Conditions.Select(condition =>
            condition ?? throw new ParseErrorException($"Rule '{Name}' has a missing condition."));
    }
}
