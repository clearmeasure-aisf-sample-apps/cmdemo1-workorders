using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Looks up creator and assignee, builds the dated batch, and persists it in one transaction
/// (#1824 keeps create-handler construction under the parameter size gate).
/// </summary>
public sealed class DatedWorkOrderBatchCreator(
    DbContext dbContext,
    IBus bus,
    IWorkOrderNumberGenerator numberGenerator,
    TimeProvider time)
{
    /// <summary>
    /// Creates the batch when both employees exist; otherwise a Failed result naming who was missing.
    /// </summary>
    public async Task<CreateDatedWorkOrdersResult> CreateAsync(
        CreateDatedWorkOrdersCommand request,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var (batch, missingEmployee) = await FindBatchAsync(request, logger, cancellationToken);
        if (batch is null)
        {
            return missingEmployee!;
        }

        var workOrders = batch.CreateWorkOrders(time.GetUtcNow().DateTime, numberGenerator);
        await SaveInOneTransactionAsync(workOrders, logger, cancellationToken);
        await PublishCreatedEventAsync(workOrders.Count, logger);
        return CreateDatedWorkOrdersResult.Created(workOrders);
    }

    private async Task<(DatedWorkOrderBatch? Batch, CreateDatedWorkOrdersResult? Failure)> FindBatchAsync(
        CreateDatedWorkOrdersCommand request,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var assignee = await FindEmployeeAsync(request.AssigneeUsername, cancellationToken);
        if (assignee is null)
        {
            return (null, NotFound(logger, "Assignee", request.AssigneeUsername));
        }

        var creator = await FindEmployeeAsync(request.CreatorUsername, cancellationToken);
        return creator is null
            ? (null, NotFound(logger, "Creator", request.CreatorUsername))
            : (new DatedWorkOrderBatch
            {
                Title = request.Title,
                Description = request.Description,
                Creator = creator,
                Assignee = assignee,
                DueDates = request.DueDates
            }, null);
    }

    private static CreateDatedWorkOrdersResult NotFound(ILogger logger, string role, string username)
    {
        logger.LogWarning("{Role} '{Username}' not found; creating zero work orders", role, username);
        return CreateDatedWorkOrdersResult.Failed($"{role} '{username}' was not found. No work orders were created.");
    }

    private async Task SaveInOneTransactionAsync(
        IReadOnlyList<WorkOrder> workOrders,
        ILogger logger,
        CancellationToken cancellationToken)
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

    private async Task PublishCreatedEventAsync(int count, ILogger logger)
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
