using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Persists multiple dated assigned work orders in one database transaction.
/// </summary>
public class CreateDatedWorkOrdersHandler(
    DbContext dbContext,
    IBus bus,
    IWorkOrderNumberGenerator numberGenerator,
    TimeProvider time,
    ILogger<CreateDatedWorkOrdersHandler> logger)
    : IRequestHandler<CreateDatedWorkOrdersCommand, CreateDatedWorkOrdersResult>
{
    public async Task<CreateDatedWorkOrdersResult> Handle(
        CreateDatedWorkOrdersCommand request,
        CancellationToken cancellationToken)
    {
        if (BatchSizeFailure(request.DueDates.Count) is { } sizeFailure)
        {
            return sizeFailure;
        }

        var (batch, missingEmployee) = await FindBatchAsync(request, cancellationToken);
        if (batch is null)
        {
            return missingEmployee!;
        }

        var workOrders = batch.CreateWorkOrders(time.GetUtcNow().DateTime, numberGenerator);
        await SaveInOneTransactionAsync(workOrders, cancellationToken);
        await PublishCreatedEventAsync(workOrders.Count);
        logger.LogInformation(
            "Created {Count} dated work orders for assignee {AssigneeUsername}",
            workOrders.Count,
            request.AssigneeUsername);
        return CreateDatedWorkOrdersResult.Created(workOrders);
    }

    private static CreateDatedWorkOrdersResult? BatchSizeFailure(int count)
    {
        if (count == 0)
        {
            return CreateDatedWorkOrdersResult.Failed("At least one due date is required.");
        }

        return count > CreateDatedWorkOrdersCommand.MaximumBatchSize
            ? CreateDatedWorkOrdersResult.Failed(
                $"A dated work-order batch cannot exceed {CreateDatedWorkOrdersCommand.MaximumBatchSize} dates.")
            : null;
    }

    /// <summary>The batch, or the failure naming the assignee or creator that was not found (assignee first).</summary>
    private async Task<(DatedWorkOrderBatch? Batch, CreateDatedWorkOrdersResult? Failure)> FindBatchAsync(
        CreateDatedWorkOrdersCommand request,
        CancellationToken cancellationToken)
    {
        var assignee = await FindEmployeeAsync(request.AssigneeUsername, cancellationToken);
        if (assignee is null)
        {
            return (null, NotFound("Assignee", request.AssigneeUsername));
        }

        var creator = await FindEmployeeAsync(request.CreatorUsername, cancellationToken);
        return creator is null
            ? (null, NotFound("Creator", request.CreatorUsername))
            : (new DatedWorkOrderBatch
            {
                Title = request.Title,
                Description = request.Description,
                Creator = creator,
                Assignee = assignee,
                DueDates = request.DueDates
            }, null);
    }

    private CreateDatedWorkOrdersResult NotFound(string role, string username)
    {
        logger.LogWarning("{Role} '{Username}' not found; creating zero work orders", role, username);
        return CreateDatedWorkOrdersResult.Failed($"{role} '{username}' was not found. No work orders were created.");
    }

    private async Task SaveInOneTransactionAsync(IReadOnlyList<WorkOrder> workOrders, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.AddRange(workOrders);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed creating dated work orders; rolling back");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task PublishCreatedEventAsync(int count)
    {
        try
        {
            await bus.Publish(new DatedWorkOrdersCreatedEvent(count));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Created {Count} dated work orders, but publishing the telemetry event failed",
                count);
        }
    }

    private async Task<Employee?> FindEmployeeAsync(string username, CancellationToken cancellationToken)
    {
        return await dbContext.Set<Employee>()
            .Include(e => e.Roles)
            .SingleOrDefaultAsync(e => e.UserName == username, cancellationToken);
    }
}
