using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.WorkOrderSpecification;

/// <summary>
/// Shared arrange steps for work-order specification handler fixtures. Builds and saves data; no assertions.
/// </summary>
public static class WorkOrderSpecificationSteps
{
    /// <summary>Builds a work order for <paramref name="creator"/> with status and optional due date.</summary>
    private static WorkOrder OrderFor(Employee creator, WorkOrderStatus status, DateOnly? due) =>
        WorkOrderBuilder.For(creator, status).DueOn(due).WithNumber(TestData.Number("S")).Build();

    /// <summary>
    /// Seeds waiting-on-vendor, plain in-progress, and complete-with-stale-waiting field for one creator.
    /// Returns the creator and the three numbers in that order.
    /// </summary>
    public static async Task<(Employee Employee, string[] Numbers)> SeedWaitingOnVendorOrdersAsync()
    {
        var employee = EmployeeBuilder.Create(TestData.UserName("vendor"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var numbers = TestData.NumberSeries(3);
        var since = new DateTime(2026, 10, 14, 9, 0, 0);
        await WorkOrderQuerySteps.SaveOrdersAsync(employee,
            WorkOrderBuilder.For(employee, WorkOrderStatus.InProgress).AssignedTo(employee)
                .WithNumber(numbers[0]).WaitingOnVendorSince(since).Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.InProgress).AssignedTo(employee)
                .WithNumber(numbers[1]).Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.Complete).AssignedTo(employee)
                .WithNumber(numbers[2]).WaitingOnVendorSince(since).Build());
        return (employee, numbers);
    }

    /// <summary>
    /// Seeds due-this-week boundary orders for a fixed clock. Returns the creator and the two included numbers.
    /// </summary>
    public static async Task<(Employee Employee, string IncludedToday, string IncludedLastDay)>
        SeedDueThisWeekOrdersAsync(TimeProvider clock)
    {
        var today = ChurchTimeZone.Today(clock);
        var employee = EmployeeBuilder.Create(TestData.UserName("dueweek"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var includedToday = OrderFor(employee, WorkOrderStatus.Assigned, today);
        var includedLastDay = OrderFor(employee, WorkOrderStatus.InProgress, today.AddDays(6));
        await WorkOrderQuerySteps.SaveOrdersAsync(employee,
            includedToday,
            includedLastDay,
            OrderFor(employee, WorkOrderStatus.Draft, today.AddDays(-1)),
            OrderFor(employee, WorkOrderStatus.Assigned, today.AddDays(7)),
            OrderFor(employee, WorkOrderStatus.Draft, null),
            OrderFor(employee, WorkOrderStatus.Complete, today),
            OrderFor(employee, WorkOrderStatus.Cancelled, today.AddDays(1)));
        return (employee, includedToday.Number!, includedLastDay.Number!);
    }
}
