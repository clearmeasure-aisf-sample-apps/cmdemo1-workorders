using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary>
/// Work orders for the SLA health parity cases. The parity database's clock is 16 November 2026, so the current church
/// month is November and the last is October. Completion times are UTC; SLA counts them by their church (America/Chicago)
/// date, and US daylight saving time ends on 1 November 2026.
/// </summary>
internal static class SlaHealthCasesMother
{
    /// <summary>Open work orders only, one of them overdue.</summary>
    public static IEnumerable<WorkOrderBuilder> NoneCompleted(AotParityDatabase database) =>
    [
        database.Order("SLA0001", WorkOrderStatus.Draft).DueOn(new DateOnly(2026, 11, 20)),
        database.Order("SLA0002", WorkOrderStatus.Assigned).DueOn(new DateOnly(2026, 11, 10)),
        database.Order("SLA0003", WorkOrderStatus.InProgress).DueOn(new DateOnly(2026, 10, 30))
    ];

    /// <summary>One on-time completion with a due date in each month, and one without a due date in each month.</summary>
    public static IEnumerable<WorkOrderBuilder> CompletedWithAndWithoutDueDate(AotParityDatabase database) =>
    [
        Completed(database, "SLA0011", new DateTime(2026, 11, 5, 15, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 11, 6)),
        Completed(database, "SLA0012", new DateTime(2026, 11, 9, 15, 0, 0, DateTimeKind.Utc), dueDate: null),
        Completed(database, "SLA0013", new DateTime(2026, 10, 14, 15, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 10, 20)),
        Completed(database, "SLA0014", new DateTime(2026, 10, 21, 15, 0, 0, DateTimeKind.Utc), dueDate: null)
    ];

    /// <summary>
    /// November: one completed late and one completed on its due date. October: one completed late. Plus an open work
    /// order past its due date, which SLA does not count.
    /// </summary>
    public static IEnumerable<WorkOrderBuilder> Overdue(AotParityDatabase database) =>
    [
        Completed(database, "SLA0021", new DateTime(2026, 11, 12, 15, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 11, 10)),
        Completed(database, "SLA0022", new DateTime(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 11, 3)),
        Completed(database, "SLA0023", new DateTime(2026, 10, 28, 15, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 10, 1)),
        database.Order("SLA0024", WorkOrderStatus.InProgress).DueOn(new DateOnly(2026, 11, 1))
    ];

    /// <summary>Completions whose UTC date and church date differ, around the month and daylight-saving boundaries.</summary>
    public static IEnumerable<WorkOrderBuilder> AcrossChurchDateBoundaries(AotParityDatabase database) =>
    [
        // 1 Nov 03:00 UTC is 31 Oct 22:00 in Chicago (CDT): October, on time for a 31 Oct due date.
        Completed(database, "SLA0031", new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 10, 31)),
        // 1 Oct 04:00 UTC is 30 Sep 23:00 in Chicago: September, outside both months.
        Completed(database, "SLA0032", new DateTime(2026, 10, 1, 4, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 9, 30)),
        // 16 Nov 05:30 UTC is 15 Nov 23:30 in Chicago (CST): on time for a 15 Nov due date, though the UTC date is later.
        Completed(database, "SLA0033", new DateTime(2026, 11, 16, 5, 30, 0, DateTimeKind.Utc), new DateOnly(2026, 11, 15))
    ];

    private static WorkOrderBuilder Completed(AotParityDatabase database, string number, DateTime completedUtc, DateOnly? dueDate) =>
        database.Order(number, WorkOrderStatus.Complete)
            .DueOn(dueDate)
            .CreatedAt(completedUtc.AddDays(-10))
            .AssignedAt(completedUtc.AddDays(-9))
            .CompletedAt(completedUtc);
}
