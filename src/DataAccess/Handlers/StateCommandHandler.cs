using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Persists work-order state commands. Existing rows are loaded and updated in place so
/// EF compares against database originals. Clear+Attach+Update left shadow FKs such as
/// AssigneeId unmodified when Cancel nulled the navigation on a detached graph (#9118).
/// </summary>
/// <remarks>
/// The source-generated mediator routes each message to a handler for its exact type, so every concrete
/// state command is listed here and all of them share <see cref="Handle(StateCommandBase, CancellationToken)"/>.
/// A new state command without a line here fails the build (Mediator diagnostic MSG0005).
/// </remarks>
public class StateCommandHandler(
    DbContext dbContext,
    TimeProvider time,
    ILogger<StateCommandHandler> logger)
    : IRequestHandler<SaveDraftCommand, StateCommandResult>,
        IRequestHandler<DraftToAssignedCommand, StateCommandResult>,
        IRequestHandler<AssignedToInProgressCommand, StateCommandResult>,
        IRequestHandler<AssignedToCancelledCommand, StateCommandResult>,
        IRequestHandler<InProgressToAssignedCommand, StateCommandResult>,
        IRequestHandler<InProgressToCompleteCommand, StateCommandResult>,
        IRequestHandler<InProgressToCancelledCommand, StateCommandResult>,
        IRequestHandler<InProgressToWaitingOnVendorCommand, StateCommandResult>,
        IRequestHandler<WaitingOnVendorToInProgressCommand, StateCommandResult>,
        IRequestHandler<WaitingOnVendorToCancelledCommand, StateCommandResult>
{
    // A field, not the primary-constructor parameter, so EF 11 can precompile the queries for Native AOT.
    private readonly DataContext _dbContext = (DataContext)dbContext;

    public ValueTask<StateCommandResult> Handle(SaveDraftCommand request, CancellationToken cancellationToken) =>
        Handle((StateCommandBase)request, cancellationToken);

    public ValueTask<StateCommandResult> Handle(DraftToAssignedCommand request, CancellationToken cancellationToken) =>
        Handle((StateCommandBase)request, cancellationToken);

    public ValueTask<StateCommandResult> Handle(AssignedToInProgressCommand request, CancellationToken cancellationToken) =>
        Handle((StateCommandBase)request, cancellationToken);

    public ValueTask<StateCommandResult> Handle(AssignedToCancelledCommand request, CancellationToken cancellationToken) =>
        Handle((StateCommandBase)request, cancellationToken);

    public ValueTask<StateCommandResult> Handle(InProgressToAssignedCommand request, CancellationToken cancellationToken) =>
        Handle((StateCommandBase)request, cancellationToken);

    public ValueTask<StateCommandResult> Handle(InProgressToCompleteCommand request, CancellationToken cancellationToken) =>
        Handle((StateCommandBase)request, cancellationToken);

    public ValueTask<StateCommandResult> Handle(InProgressToCancelledCommand request, CancellationToken cancellationToken) =>
        Handle((StateCommandBase)request, cancellationToken);

    public ValueTask<StateCommandResult> Handle(InProgressToWaitingOnVendorCommand request, CancellationToken cancellationToken) =>
        Handle((StateCommandBase)request, cancellationToken);

    public ValueTask<StateCommandResult> Handle(WaitingOnVendorToInProgressCommand request, CancellationToken cancellationToken) =>
        Handle((StateCommandBase)request, cancellationToken);

    public ValueTask<StateCommandResult> Handle(WaitingOnVendorToCancelledCommand request, CancellationToken cancellationToken) =>
        Handle((StateCommandBase)request, cancellationToken);

    public async ValueTask<StateCommandResult> Handle(StateCommandBase request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Executing");
        var currentDateTime = time.GetUtcNow().DateTime;
        var beginStatus = request.GetBeginStatus();
        request.Execute(new StateCommandContext { CurrentDateTime = currentDateTime });

        var order = request.WorkOrder;
        if (order.Assignee == order.Creator)
        {
            order.Assignee = order.Creator; //EFCore reference checking
        }

        var transition = WorkOrderStatusTransition.Of(request, beginStatus, currentDateTime);
        var persisted = await PersistAsync(order, transition, cancellationToken);

        var loweredTransitionVerb = request.TransitionVerbPastTense.ToLower();
        var workOrderNumber = persisted.Number;
        var fullName = request.CurrentUser.GetFullName();

        var debugMessage = $"{fullName} has {loweredTransitionVerb} work order {workOrderNumber}";
        logger.LogDebug(debugMessage);
        logger.LogInformation("Executed");

        var result = new StateCommandResult(persisted, request.TransitionVerbPresentTense, debugMessage);

        return result;
    }

    private async Task<WorkOrder> PersistAsync(
        WorkOrder order,
        WorkOrderStatusTransition transition,
        CancellationToken cancellationToken)
    {
        _dbContext.ChangeTracker.Clear();
        var persisted = order.Id == Guid.Empty
            ? await InsertNewAsync(order, cancellationToken)
            : await UpdateExistingAsync(order, cancellationToken);

        // Recorded in the same SaveChanges (one transaction) as the work order.
        if (transition.ChangeOf(persisted) is { } statusChange)
        {
            _dbContext.Add(statusChange);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return persisted;
    }

    private async Task<WorkOrder> InsertNewAsync(WorkOrder order, CancellationToken cancellationToken)
    {
        var number = order.Number;
        var ct = cancellationToken;
        if (await _dbContext.Set<WorkOrder>().AnyAsync(w => w.Number == number, ct))
        {
            throw new InvalidOperationException($"Work order number {order.Number} already exists.");
        }

        _dbContext.Attach(order);
        _dbContext.Add(order);
        return order;
    }

    private async Task<WorkOrder> UpdateExistingAsync(WorkOrder order, CancellationToken cancellationToken)
    {
        var id = order.Id;
        var ct = cancellationToken;
        var existing = await _dbContext.Set<WorkOrder>()
            .SingleAsync(workOrder => workOrder.Id == id, ct);

        _dbContext.Entry(existing).CurrentValues.SetValues(order);
        // Attachments remain database-owned; remoted commands do not carry a complete collection.
        await ApplyRelationshipsAsync(existing, order, cancellationToken);
        return existing;
    }

    private async Task ApplyRelationshipsAsync(
        WorkOrder target,
        WorkOrder source,
        CancellationToken cancellationToken)
    {
        if (source.Creator != null)
        {
            target.Creator = await ResolveEmployeeAsync(source.Creator.Id, cancellationToken);
        }

        target.Assignee = source.Assignee == null
            ? null
            : await ResolveEmployeeAsync(source.Assignee.Id, cancellationToken);
    }

    private async Task<Employee> ResolveEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var tracked = _dbContext.Set<Employee>().Local
            .FirstOrDefault(employee => employee.Id == employeeId);
        if (tracked != null)
        {
            return tracked;
        }

        var id = employeeId;
        var ct = cancellationToken;
        return await _dbContext.Set<Employee>()
            .SingleAsync(employee => employee.Id == id, ct);
    }
}
