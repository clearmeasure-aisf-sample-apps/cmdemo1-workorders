using ClearMeasure.Bootcamp.Core.Validation;
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

    /// <summary>User-visible message when the due-date list is empty.</summary>
    public const string AtLeastOneDueDateRequired = "At least one due date is required.";

    /// <summary>User-visible message when the due-date list is larger than <see cref="MaximumBatchSize"/>.</summary>
    public const string BatchCannotExceed = "A dated work-order batch cannot exceed 10 dates.";

    /// <summary>
    /// In-process validation (#1824): DueDates empty / over-ten keep master's Failed messages; other
    /// shape rules use <c>CreateDatedWorkOrdersCommandValidator</c> messages. An empty list means the
    /// handler may look up creator and assignee. Callers receive these as
    /// <see cref="CreateDatedWorkOrdersResult.Failed"/> — never a FluentValidation exception.
    /// </summary>
    public IReadOnlyList<string> FindRuleViolations()
    {
        if (DueDates.Count == 0)
        {
            return [AtLeastOneDueDateRequired];
        }

        if (DueDates.Count > MaximumBatchSize)
        {
            return [BatchCannotExceed];
        }

        return InProcessCreateCommandShapeValidation.ErrorMessages(this);
    }
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
