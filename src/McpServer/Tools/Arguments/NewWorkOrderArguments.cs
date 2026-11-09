using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

/// <summary>
/// The arguments of create-work-order that describe the new draft. A description is required; the due date is an
/// optional <c>yyyy-MM-dd</c> date; priority and building follow <see cref="PriorityArgument"/> and
/// <see cref="BuildingArgument"/>.
/// </summary>
internal sealed record NewWorkOrderArguments(
    string Title,
    string Description,
    string? RoomNumber,
    string? DueDate,
    string? Instructions,
    string? Priority,
    string? Building)
{
    public string? Error =>
        DescriptionError(Description)
        ?? DueDateError()
        ?? PriorityArgument.Parse(Priority).Error
        ?? BuildingArgument.Parse(Building).Error;

    public WorkOrder ToDraft(Employee creator, string number)
    {
        DatedWorkOrderScheduling.TryParseOptionalDueDate(DueDate, out var dueDate, out _);
        return new WorkOrder
        {
            Title = Title,
            Description = Description,
            Instructions = Instructions,
            Creator = creator,
            Status = WorkOrderStatus.Draft,
            Number = number,
            RoomNumber = RoomNumber,
            DueDate = dueDate,
            Priority = PriorityArgument.Parse(Priority).Value,
            Building = BuildingArgument.Parse(Building).Value
        };
    }

    /// <summary>A work-order description is required and at most <see cref="WorkOrder.DescriptionMaxLength"/> characters.</summary>
    public static string? DescriptionError(string? description) =>
        string.IsNullOrWhiteSpace(description) ? "Description is required." : DescriptionLengthError(description);

    public static string? DescriptionLengthError(string? description) =>
        description is { Length: > WorkOrder.DescriptionMaxLength }
            ? $"Description cannot exceed {WorkOrder.DescriptionMaxLength} characters."
            : null;

    private string? DueDateError() =>
        DatedWorkOrderScheduling.TryParseOptionalDueDate(DueDate, out _, out var error) ? null : error;
}
