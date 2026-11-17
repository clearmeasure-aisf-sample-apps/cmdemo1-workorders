using OpenFeature.Constant;
using OpenFeature.Error;
using OpenFeature.Model;

namespace ClearMeasure.Bootcamp.DataAccess.FeatureFlags;

/// <summary>
/// One feature toggle from the Octopus v4 evaluations response: resolved by the server (<see cref="Value"/> and
/// <see cref="Reason"/>) or deferred to the client (<see cref="EvaluationKey"/> and <see cref="Rules"/>). The checks
/// and reasons follow Octopus.OpenFeature 4.0.0 so the interim provider resolves a toggle the same way.
/// </summary>
internal sealed record OctopusFlagEvaluation
{
    /// <summary>The toggle's slug, matched without regard to case.</summary>
    public string? Slug { get; init; }

    /// <summary>The server-resolved value, when the server resolved the toggle.</summary>
    public bool? Value { get; init; }

    /// <summary>Why the server resolved the toggle to <see cref="Value"/>.</summary>
    public string? Reason { get; init; }

    /// <summary>The key Octopus pairs with a targeting key for percentage rollouts, when the client resolves the toggle.</summary>
    public string? EvaluationKey { get; init; }

    /// <summary>The client-side rules, in order; the toggle is on when one matches.</summary>
    public OctopusFlagRule?[]? Rules { get; init; }

    /// <summary>The toggle's value for <paramref name="context"/>; throws <see cref="ParseErrorException"/> when malformed.</summary>
    public ResolutionDetails<bool> Resolve(EvaluationContext? context)
    {
        if (Value is { } value)
        {
            if (Reason is null)
            {
                throw new ParseErrorException("The flag has a value but has no reason.");
            }

            if (EvaluationKey is not null || Rules is not null)
            {
                throw new ParseErrorException("The flag has both a server-resolved value and client-side rules.");
            }

            return Resolved(value, Reason);
        }

        foreach (var rule in DeferredRules())
        {
            if (rule.Matches(context))
            {
                return Resolved(true, $"Matched rule '{rule.Name}'.");
            }
        }

        return Resolved(false, "Did not match any rules.");
    }

    /// <summary>The rules of a toggle deferred to the client, after checking the deferred shape is complete.</summary>
    private IEnumerable<OctopusFlagRule> DeferredRules()
    {
        if (Rules is null)
        {
            throw new ParseErrorException("The flag has neither a value nor rules.");
        }

        if (EvaluationKey is null)
        {
            throw new ParseErrorException("The flag defers to the client but has no evaluation key.");
        }

        if (Rules.Length == 0)
        {
            throw new ParseErrorException("The flag defers to the client with no rules.");
        }

        return Rules.Select(rule => rule ?? throw new ParseErrorException("The flag has a missing rule."));
    }

    private ResolutionDetails<bool> Resolved(bool value, string reason) => new(Slug!, value, ErrorType.None, reason);
}
