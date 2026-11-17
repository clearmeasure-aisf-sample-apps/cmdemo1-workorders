using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

/// <summary>
/// Stub bus that returns only the matching work order when the specification status is Assigned.
/// </summary>
public sealed class StubStatusFilteredBus(WorkOrder matching, WorkOrder other) : StubBus
{
    /// <inheritdoc />
    public override async Task<TResponse> Send<TResponse>(Mediator.IRequest<TResponse> request)
    {
        if (request is not WorkOrderSpecificationQuery specification)
        {
            return await base.Send(request);
        }

        _ = await base.Send(request);
        var rows = specification.StatusKey == WorkOrderStatus.Assigned.Key
            ? new[] { matching }
            : new[] { matching, other };
        return (TResponse)(object)rows;
    }
}
