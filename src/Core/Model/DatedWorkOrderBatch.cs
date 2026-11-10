using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// One assigned work order per due date, all with the same title, description, creator and assignee: the schedule a
/// <see cref="StateCommands.CreateDatedWorkOrdersCommand"/> asks for once its employees are found.
/// </summary>
public sealed record DatedWorkOrderBatch
{
    public required string Title { get; init; }

    public required string Description { get; init; }

    public required Employee Creator { get; init; }

    public required Employee Assignee { get; init; }

    public required IReadOnlyList<DateOnly> DueDates { get; init; }

    /// <summary>Creates the work orders, created and assigned at <paramref name="now"/>, numbered in due-date order.</summary>
    public IReadOnlyList<WorkOrder> CreateWorkOrders(DateTime now, IWorkOrderNumberGenerator numberGenerator) =>
        DueDates.Select(dueDate => new WorkOrder
        {
            Number = numberGenerator.GenerateNumber(),
            Title = Title,
            Description = Description,
            Creator = Creator,
            Assignee = Assignee,
            Status = WorkOrderStatus.Assigned,
            CreatedDate = now,
            AssignedDate = now,
            DueDate = dueDate
        }).ToList();
}
