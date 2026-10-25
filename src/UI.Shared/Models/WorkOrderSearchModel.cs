using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

public class WorkOrderSearchModel
{
    public SearchFilters Filters { get; } = new();
    public WorkOrderSearchResultRow[] Results { get; set; } = [];

    /// <summary>Values bound to the filter controls on the search page.</summary>
    public class SearchFilters
    {
        public string? Creator { get; set; }
        public string? Assignee { get; set; }
        public string? Status { get; set; }
        public bool OverdueOnly { get; set; }

        /// <summary>When true only In Progress work orders waiting on a vendor are listed.</summary>
        public bool WaitingOnVendorOnly { get; set; }

        /// <summary>Selected priority name (Low, Medium, High) or empty for all.</summary>
        public string? Priority { get; set; }

        /// <summary>Selected building or empty for all.</summary>
        public string? Building { get; set; }

        /// <summary>Room number to match exactly, or empty for every room.</summary>
        public string? Room { get; set; }
    }
}

/// <summary>
/// Search row projection including read-time due-date display and urgency.
/// </summary>
public class WorkOrderSearchResultRow
{
    public required WorkOrder WorkOrder { get; init; }
    public string Number => WorkOrder.Number ?? string.Empty;
    public Employee? Creator => WorkOrder.Creator;
    public Employee? Assignee => WorkOrder.Assignee;
    public WorkOrderStatus Status => WorkOrder.Status;
    public string? Title => WorkOrder.Title;
    public string? DueDateDisplay { get; init; }
    public string DueDateCssClass { get; init; } = string.Empty;
    public string? DueDateUrgencyText { get; init; }
    public DueDateUrgency Urgency { get; init; }

    /// <summary>Number shown in the table, for example "WO-38173BA"; the stored number stays unprefixed.</summary>
    public string DisplayNumber => "WO-" + Number;

    /// <summary>Status pill text and style; "Overdue" for open work orders past their due date.</summary>
    public StatusPill StatusPill => StatusPillPresenter.Present(WorkOrder, Urgency == DueDateUrgency.Overdue);

    /// <summary>"priority--high", "priority--medium" or "priority--low".</summary>
    public string PriorityCssClass => "priority--" + WorkOrder.Priority.ToString().ToLowerInvariant();

    /// <summary>Initials of the assignee for the avatar.</summary>
    public string AssigneeInitials => UserDisplay.Initials(Assignee);

    /// <summary>One of six avatar colours, stable for a given assignee.</summary>
    public string AvatarCssClass => "avatar--" + AvatarIndex(Assignee?.UserName);

    private static int AvatarIndex(string? userName) =>
        (userName ?? string.Empty).Aggregate(0, (hash, c) => (hash * 31 + c) % 6);
}
