using ClearMeasure.Bootcamp.Core.FeatureFlags;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns one entry per building, each with its rooms and, per room, the open, overdue, recently
/// completed and total work orders. Buildings and rooms are grouped by the database with the same
/// trimming and collation as the Work Order Search <c>Building</c> and <c>Room</c> filters, and the
/// counts use the same predicates as the search's summary cards, so each count equals the total of a
/// Work Order Search for that building and room. An empty building or room name means "no building"
/// or "no room" (null, empty or blank), matching the search's <c>NoBuilding</c> and <c>NoRoom</c>.
/// Requires <see cref="FeatureFlagNames.BuildingManagement"/>; the bus denies this query when the flag is off.
/// </summary>
[RequiresFeature(FeatureFlagNames.BuildingManagement)]
public record BuildingRoomSummaryQuery : IRequest<BuildingSummary[]>, IRemotableRequest;
