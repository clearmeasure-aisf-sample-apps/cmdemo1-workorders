using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// The most recent dated event of a work order (created, assigned or completed), used to list
/// recent activity without an audit table.
/// </summary>
/// <param name="Verb">The event name shown to people, for example "Completed".</param>
/// <param name="WhenUtc">When the event happened, in UTC.</param>
public sealed record WorkOrderActivity(string Verb, DateTime WhenUtc)
{
    private const string CompletedVerb = "Completed";
    private const string AssignedVerb = "Assigned";
    private const string CreatedVerb = "Created";

    /// <summary>
    /// Gets the church calendar date of the event; see <see cref="ChurchTimeZone.ChurchDate"/>.
    /// </summary>
    public DateOnly ChurchDate => ChurchTimeZone.ChurchDate(WhenUtc);

    /// <summary>
    /// Returns the latest of the order's completed, assigned and created dates, or null when it has
    /// none. On equal dates <see cref="Precedence"/> decides: Completed over Assigned, Assigned
    /// over Created.
    /// </summary>
    public static WorkOrderActivity? Latest(WorkOrder workOrder)
    {
        WorkOrderActivity?[] candidates =
        [
            Candidate(CompletedVerb, workOrder.CompletedDate),
            Candidate(AssignedVerb, workOrder.AssignedDate),
            Candidate(CreatedVerb, workOrder.CreatedDate)
        ];
        return candidates
            .OfType<WorkOrderActivity>()
            .OrderByDescending(a => a.WhenUtc)
            .ThenByDescending(a => Precedence(a.Verb))
            .FirstOrDefault();
    }

    /// <summary>
    /// The tie-break for equal dates, highest first: Completed, Assigned, Created. Stated as a
    /// value so the winner never depends on candidate order or on how a max function resolves ties.
    /// </summary>
    private static int Precedence(string verb) =>
        verb switch
        {
            CompletedVerb => 3,
            AssignedVerb => 2,
            CreatedVerb => 1,
            _ => 0
        };

    private static WorkOrderActivity? Candidate(string verb, DateTime? when) =>
        when == null ? null : new WorkOrderActivity(verb, when.Value);
}
