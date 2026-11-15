using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>
/// Creates multiple dated, assigned work orders in a single transaction.
/// </summary>
public record CreateDatedWorkOrdersCommand(
    string CreatorUsername,
    string AssigneeUsername,
    [property: ExcludeFromBusActivity] string Title,
    [property: ExcludeFromBusActivity] string Description,
    IReadOnlyList<DateOnly> DueDates) : IRequest<CreateDatedWorkOrdersResult>, IRemotableRequest
{
    /// <summary>
    /// Maximum work orders allowed in one transactional create.
    /// </summary>
    public const int MaximumBatchSize = 10;
}

/// <summary>
/// Result of a transactional dated work-order create.
/// </summary>
public record CreateDatedWorkOrdersResult(
    bool Success,
    string Message,
    IReadOnlyList<CreatedDatedWorkOrder> WorkOrders)
{
    /// <summary>A batch that created nothing, and why.</summary>
    public static CreateDatedWorkOrdersResult Failed(string message) => new(false, message, []);

    public static CreateDatedWorkOrdersResult Created(IReadOnlyList<WorkOrder> workOrders) =>
        new(true,
            $"Created {workOrders.Count} work orders.",
            workOrders.Select(workOrder => new CreatedDatedWorkOrder(workOrder.Number!, workOrder.DueDate!.Value)).ToList());
}

/// <summary>
/// Summary of one work order created by <see cref="CreateDatedWorkOrdersCommand"/>.
/// </summary>
public record CreatedDatedWorkOrder(string Number, DateOnly DueDate);
