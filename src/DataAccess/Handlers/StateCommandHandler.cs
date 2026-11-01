using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Persists work-order state commands. Existing rows are loaded and updated in place so
/// EF compares against database originals. Clear+Attach+Update left shadow FKs such as
/// AssigneeId unmodified when Cancel nulled the navigation on a detached graph (#9118).
/// </summary>
public class StateCommandHandler(
    DbContext dbContext,
    TimeProvider time,
    IDistributedBus distributedBus,
    ILogger<StateCommandHandler> logger)
    : IRequestHandler<StateCommandBase, StateCommandResult>
{
    public async Task<StateCommandResult> Handle(StateCommandBase request,
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

        var persisted = await PersistAsync(order, beginStatus, request.CurrentUser, currentDateTime, request, cancellationToken);

        var loweredTransitionVerb = request.TransitionVerbPastTense.ToLower();
        var workOrderNumber = persisted.Number;
        var fullName = request.CurrentUser.GetFullName();

        var debugMessage = $"{fullName} has {loweredTransitionVerb} work order {workOrderNumber}";
        logger.LogDebug(debugMessage);
        logger.LogInformation("Executed");

        var result = new StateCommandResult(persisted, request.TransitionVerbPresentTense, debugMessage);

        await distributedBus.PublishAsync(request.StateTransitionEvent, cancellationToken);
        return result;
    }

    private async Task<WorkOrder> PersistAsync(
        WorkOrder order,
        WorkOrderStatus beginStatus,
        Employee changedBy,
        DateTime changedDate,
        StateCommandBase request,
        CancellationToken cancellationToken)
    {
        if (order.Id == Guid.Empty)
        {
            return await InsertNewAsync(order, beginStatus, changedBy, changedDate, request, cancellationToken);
        }

        return await UpdateExistingAsync(order, beginStatus, changedBy, changedDate, request, cancellationToken);
    }

    private async Task<WorkOrder> InsertNewAsync(
        WorkOrder order,
        WorkOrderStatus beginStatus,
        Employee changedBy,
        DateTime changedDate,
        StateCommandBase request,
        CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        if (await dbContext.Set<WorkOrder>().AnyAsync(w => w.Number == order.Number, cancellationToken))
        {
            throw new InvalidOperationException($"Work order number {order.Number} already exists.");
        }

        dbContext.Attach(order);
        dbContext.Add(order);

        // Record status change if this is a state-changing command, in the same transaction
        if (order.Status != beginStatus)
        {
            var statusChange = new WorkOrderStatusChange
            {
                Id = Guid.NewGuid(),
                WorkOrderId = order.Id,
                FromStatus = beginStatus,
                ToStatus = order.Status,
                ChangedById = changedBy.Id,
                ChangedDate = changedDate,
                Reason = ShelveReason(request)
            };
            dbContext.Add(statusChange);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return order;
    }

    private async Task<WorkOrder> UpdateExistingAsync(
        WorkOrder order,
        WorkOrderStatus beginStatus,
        Employee changedBy,
        DateTime changedDate,
        StateCommandBase request,
        CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();

        var existing = await dbContext.Set<WorkOrder>()
            .SingleAsync(workOrder => workOrder.Id == order.Id, cancellationToken);

        dbContext.Entry(existing).CurrentValues.SetValues(order);
        // Attachments remain database-owned; remoted commands do not carry a complete collection.
        await ApplyRelationshipsAsync(existing, order, cancellationToken);

        // Record status change if this is a state-changing command, in the same transaction
        if (existing.Status != beginStatus)
        {
            var statusChange = new WorkOrderStatusChange
            {
                Id = Guid.NewGuid(),
                WorkOrderId = existing.Id,
                FromStatus = beginStatus,
                ToStatus = existing.Status,
                ChangedById = changedBy.Id,
                ChangedDate = changedDate,
                Reason = ShelveReason(request)
            };
            dbContext.Add(statusChange);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private static string? ShelveReason(StateCommandBase request)
    {
        if (request is not InProgressToAssignedCommand { Reason: { } reason })
        {
            return null;
        }

        var trimmed = reason.Trim();
        return trimmed.Length == 0 ? null : trimmed;
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
        var tracked = dbContext.Set<Employee>().Local
            .FirstOrDefault(employee => employee.Id == employeeId);
        if (tracked != null)
        {
            return tracked;
        }

        return await dbContext.Set<Employee>()
            .SingleAsync(employee => employee.Id == employeeId, cancellationToken);
    }
}
