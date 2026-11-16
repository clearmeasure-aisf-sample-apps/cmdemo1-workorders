using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

/// <summary>
/// Stub bus that holds subsequent specification queries until a test completes each held task.
/// </summary>
public sealed class StubHeldSearchBus : StubBus
{
    private bool _initialSearchDone;

    /// <summary>Completion sources for specification searches after the first.</summary>
    public List<TaskCompletionSource<WorkOrder[]>> Held { get; } = [];

    /// <inheritdoc />
    public override async Task<TResponse> Send<TResponse>(MediatR.IRequest<TResponse> request)
    {
        if (request is not WorkOrderSpecificationQuery || !_initialSearchDone)
        {
            _initialSearchDone |= request is WorkOrderSpecificationQuery;
            return await base.Send(request);
        }

        var pending = new TaskCompletionSource<WorkOrder[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        Held.Add(pending);
        return (TResponse)(object)await pending.Task;
    }
}
