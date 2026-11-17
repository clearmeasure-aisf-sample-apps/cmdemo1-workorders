using ClearMeasure.Bootcamp.DataAccess.FeatureFlags;
using OpenFeature.Model;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.FeatureFlags;

/// <summary>Octopus toggles, rules, conditions and evaluation contexts, as the v4 evaluations response describes them.</summary>
internal static class OctopusFlagMother
{
    public static OctopusFlagEvaluation ServerResolved(bool value, string? reason = "The flag is enabled for this environment.") =>
        new() { Slug = "toggle", Value = value, Reason = reason };

    public static OctopusFlagEvaluation Deferred(params OctopusFlagRule?[]? rules) =>
        new() { Slug = "toggle", EvaluationKey = "evaluation-key", Rules = rules };

    public static OctopusFlagRule Rule(string? name, params OctopusFlagCondition?[]? conditions) => new(name, conditions);

    public static OctopusFlagCondition IsOneOf(string? key, params string?[]? values) =>
        new("context-attribute-is-one-of", key, values, null);

    public static OctopusFlagCondition IsNotOneOf(string? key, params string?[]? values) =>
        new("context-attribute-is-not-one-of", key, values, null);

    public static OctopusFlagCondition Percentage(int? percentage) => new("percentage-by-context", null, null, percentage);

    public static OctopusFlagCondition OfType(string? type) => new(type, "region", ["eu"], 100);

    public static EvaluationContext InRegion(string region) => EvaluationContext.Builder().Set("region", region).Build();

    public static EvaluationContext WithTargetingKey(string key) => EvaluationContext.Builder().SetTargetingKey(key).Build();
}
