using OpenFeature;
using OpenFeature.Error;
using OpenFeature.Model;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess;

/// <summary>
/// OpenFeature provider that targets the early-access banner toggle by evaluation context: phase 1 answers true
/// only for <see cref="EarlyAccessUser"/>, and once <see cref="LaterPhase"/> is on it answers true for everyone.
/// Every other flag key is unknown, so the evaluator moves on to its next key.
/// </summary>
public sealed class TargetingStubFeatureProvider : FeatureProvider
{
    /// <summary>The one key this provider knows: the compact Octopus slug of EarlyAccessBanner.</summary>
    private const string EarlyAccessKey = "earlyaccessbanner";

    /// <summary>The rollout user of phase 1.</summary>
    public const string EarlyAccessUser = "tlovejoy";

    /// <summary>The username of a colleague who is not in the rollout yet.</summary>
    public const string OtherUser = "gwillie";

    /// <summary>Every context this provider was asked to evaluate the early-access key with, in order.</summary>
    public List<EvaluationContext> Contexts { get; } = [];

    /// <summary>Turns on the later phase, where the toggle is enabled for everyone.</summary>
    public bool LaterPhase { get; init; }

    /// <inheritdoc />
    public override Metadata GetMetadata() => new("Targeting Stub");

    /// <inheritdoc />
    public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
        string flagKey,
        bool defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (flagKey != EarlyAccessKey)
        {
            throw new FlagNotFoundException($"unknown flag {flagKey}");
        }

        if (context is null)
        {
            throw new GeneralException("the evaluator must pass an evaluation context");
        }

        Contexts.Add(context);
        var enabled = LaterPhase || context.TargetingKey == EarlyAccessUser;
        return Task.FromResult(new ResolutionDetails<bool>(flagKey, enabled));
    }

    /// <inheritdoc />
    public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
        string flagKey,
        string defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("the early-access toggle is boolean");

    /// <inheritdoc />
    public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
        string flagKey,
        int defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("the early-access toggle is boolean");

    /// <inheritdoc />
    public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
        string flagKey,
        double defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("the early-access toggle is boolean");

    /// <inheritdoc />
    public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
        string flagKey,
        Value defaultValue,
        EvaluationContext? context = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("the early-access toggle is boolean");
}
