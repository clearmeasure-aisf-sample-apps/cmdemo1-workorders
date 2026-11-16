using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

/// <summary>
/// Stub bus that serves bulk assign queries and records state commands.
/// </summary>
// ReSharper disable once ConvertToPrimaryConstructor -- the orders are also passed to the base constructor (CS9107)
public sealed class StubBulkBus : StubBus
{
    private readonly WorkOrder[] _orders;

    /// <summary>Creates a bulk stub seeded with <paramref name="orders"/>.</summary>
    public StubBulkBus(WorkOrder[] orders) : base(orders)
    {
        _orders = orders;
    }

    /// <summary>State commands sent during bulk actions.</summary>
    public List<StateCommandBase> Commands { get; } = [];

    /// <inheritdoc />
    public override Task<TResponse> Send<TResponse>(Mediator.IRequest<TResponse> request)
    {
        if (request is WorkOrdersByNumbersQuery byNumbers)
        {
            var wanted = new HashSet<string>(byNumbers.Numbers, StringComparer.Ordinal);
            var matches = _orders.Where(order => order.Number != null && wanted.Contains(order.Number)).ToArray();
            return Task.FromResult((TResponse)(object)matches);
        }

        if (request is WorkOrderByNumberQuery byNumber)
        {
            return Task.FromResult((TResponse)(object)_orders.Single(o => o.Number == byNumber.Number));
        }

        if (request is StateCommandBase command)
        {
            Commands.Add(command);
            return Task.FromResult((TResponse)(object)new StateCommandResult(command.WorkOrder, command.TransitionVerbPresentTense, "done"));
        }

        return base.Send(request);
    }
}
