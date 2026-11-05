using ClearMeasure.Bootcamp.Core.FeatureFlags;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns the runtime value of every <see cref="FeatureFlagNames.All"/> entry so callers can hide UI for
/// disabled features. Enforcement stays in the bus feature flag behavior.
/// </summary>
public record FeatureFlagsQuery : IRequest<FeatureFlagsResult>, IRemotableRequest;

/// <summary>Flag name to enabled, one entry per catalog name.</summary>
public record FeatureFlagsResult(IReadOnlyDictionary<string, bool> Flags);
