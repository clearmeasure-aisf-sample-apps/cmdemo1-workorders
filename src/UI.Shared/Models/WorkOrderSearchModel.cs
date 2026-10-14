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

        /// <summary>Selected priority name (Low, Medium, High) or empty for all.</summary>
        public string? Priority { get; set; }

        /// <summary>Selected building or empty for all.</summary>
        public string? Building { get; set; }
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
    public string StatusBadgeCssClass { get; init; } = string.Empty;
}
