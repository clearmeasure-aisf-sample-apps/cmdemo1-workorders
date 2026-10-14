using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns the <paramref name="Count"/> work orders changed most recently, by the latest of their
/// created, assigned and completed dates, newest first, ties broken by number.
/// </summary>
/// <param name="Count">How many work orders to return (1 to 50).</param>
public record RecentWorkOrdersQuery(int Count) : IRequest<WorkOrder[]>, IRemotableRequest;
