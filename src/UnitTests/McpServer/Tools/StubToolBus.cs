using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools;

/// <summary>
/// An <see cref="IBus"/> for the MCP tool fixtures: answers work-order and employee lookups from what was added
/// (an unknown username throws, like the real handler), saves drafts, and records every request.
/// </summary>
public sealed class StubToolBus : IBus
{
    private readonly Dictionary<string, WorkOrder> _workOrders = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Employee> _employees = new(StringComparer.Ordinal);

    public List<object> SentRequests { get; } = [];

    public StubToolBus With(WorkOrder workOrder)
    {
        _workOrders[workOrder.Number!] = workOrder;
        return With(workOrder.Creator!);
    }

    public StubToolBus With(Employee employee)
    {
        _employees[employee.UserName] = employee;
        return this;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        SentRequests.Add(request);
        object? response = request switch
        {
            WorkOrderByNumberQuery query => _workOrders.GetValueOrDefault(query.Number),
            EmployeeByUserNameQuery query => _employees.TryGetValue(query.Username, out var employee)
                ? employee
                : throw new InvalidOperationException($"No employee '{query.Username}'."),
            SaveDraftCommand command => new StateCommandResult(command.WorkOrder),
            _ => throw new NotSupportedException(request.GetType().Name)
        };
        return Task.FromResult((TResponse)response!);
    }

    public Task<object?> Send(object request) => throw new NotSupportedException();

    public Task Publish(INotification notification) => Task.CompletedTask;
}
