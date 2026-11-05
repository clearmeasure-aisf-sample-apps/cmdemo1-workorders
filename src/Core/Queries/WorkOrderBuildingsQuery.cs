using ClearMeasure.Bootcamp.Core.FeatureFlags;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns the distinct, non-blank building names used by work orders, trimmed and sorted.
/// Requires <see cref="FeatureFlagNames.BuildingManagement"/>; the bus denies this query when the flag is off.
/// </summary>
[RequiresFeature(FeatureFlagNames.BuildingManagement)]
public record WorkOrderBuildingsQuery : IRequest<string[]>, IRemotableRequest;
