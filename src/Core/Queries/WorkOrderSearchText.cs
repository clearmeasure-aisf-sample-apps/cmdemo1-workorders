using System.Linq.Expressions;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Free-text match over number, title, room and creator/assignee names.
/// </summary>
public static class WorkOrderSearchText
{
    /// <summary>True when number, title, room or a party name contains <paramref name="q"/> (lowered).</summary>
    public static Expression<Func<WorkOrder, bool>> Matches(string q) =>
        WorkOrderExpression.OrElse(NumberTitleOrRoomContains(q), CreatorOrAssigneeNameContains(q));

    private static Expression<Func<WorkOrder, bool>> NumberTitleOrRoomContains(string q) =>
        wo => (wo.Number != null && wo.Number.ToLower().Contains(q)) ||
              (wo.Title != null && wo.Title.ToLower().Contains(q)) ||
              (wo.RoomNumber != null && wo.RoomNumber.ToLower().Contains(q));

    private static Expression<Func<WorkOrder, bool>> CreatorOrAssigneeNameContains(string q) =>
        wo => (wo.Creator != null && (wo.Creator.FirstName.ToLower().Contains(q) || wo.Creator.LastName.ToLower().Contains(q))) ||
              (wo.Assignee != null && (wo.Assignee.FirstName.ToLower().Contains(q) || wo.Assignee.LastName.ToLower().Contains(q)));
}
