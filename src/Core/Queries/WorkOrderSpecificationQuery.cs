using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkOrderSpecificationQuery : IRequest<WorkOrder[]>, IRemotableRequest
{
    public void MatchStatus(WorkOrderStatus? status)
    {
        StatusKey = status?.Key;
    }

    public void MatchAssignee(Employee? assignee)
    {
        Assignee = assignee;
    }

    public void MatchCreator(Employee? creator)
    {
        Creator = creator;
    }

    public void MatchOverdueOnly(bool overdueOnly)
    {
        OverdueOnly = overdueOnly;
    }

    /// <summary>Restricts the result to one priority; null matches every priority.</summary>
    public void MatchPriority(WorkOrderPriority? priority)
    {
        Priority = priority;
    }

    /// <summary>Restricts the result to one building; null or blank matches every building.</summary>
    public void MatchBuilding(string? building)
    {
        Building = building;
    }

    /// <summary>Restricts the result to open work orders (Draft, Assigned, In Progress).</summary>
    public void MatchOpenOnly(bool openOnly)
    {
        OpenOnly = openOnly;
    }

    /// <summary>Restricts the result to work orders completed within the last <paramref name="days"/> days.</summary>
    public void MatchCompletedWithinDays(int? days)
    {
        CompletedWithinDays = days;
    }

    /// <summary>
    /// Restricts the result to work orders whose number, title, room, creator or assignee name contains
    /// the text, ignoring case.
    /// </summary>
    public void MatchSearchText(string? searchText)
    {
        SearchText = searchText;
    }

    public string? StatusKey { get; set; }

    // ReSharper disable once MemberCanBePrivate.Global -- required for System.Text.Json round-trip serialization (remoting protocol)
    public Employee? Assignee { get; set; }

    // ReSharper disable once MemberCanBePrivate.Global -- required for System.Text.Json round-trip serialization (remoting protocol)
    public Employee? Creator { get; set; }

    // ReSharper disable once MemberCanBePrivate.Global -- required for System.Text.Json round-trip serialization (remoting protocol)
    public bool OverdueOnly { get; set; }

    /// <summary>Priority to match; null matches every priority.</summary>
    public WorkOrderPriority? Priority { get; set; }

    /// <summary>Building to match; null or blank matches every building.</summary>
    public string? Building { get; set; }

    /// <summary>When true only Draft, Assigned and In Progress work orders match.</summary>
    public bool OpenOnly { get; set; }

    /// <summary>When set only work orders completed within this many days match.</summary>
    public int? CompletedWithinDays { get; set; }

    /// <summary>Free text matched against number, title, room, creator and assignee names.</summary>
    public string? SearchText { get; set; }

    public WorkOrderStatus? Status => StatusKey != null ? WorkOrderStatus.FromKey(StatusKey) : null;
}
