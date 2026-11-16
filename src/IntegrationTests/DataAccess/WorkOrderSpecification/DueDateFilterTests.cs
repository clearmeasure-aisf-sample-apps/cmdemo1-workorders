using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.WorkOrderSpecification;

[TestFixture]
public class DueDateFilterTests
{
    [Test]
    public async Task ShouldReturnOnlyOverdueWorkOrders_WhenOverdueOnlyFilterIsTrue()
    {
        var overdueOrderNumber = TestData.Number("S");
        var futureOrderNumber = TestData.Number("S");
        var employee = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(employee,
            WorkOrderBuilder.For(employee, WorkOrderStatus.InProgress)
                .WithNumber(overdueOrderNumber).DueOn(new DateOnly(2000, 1, 1)).Build(),
            WorkOrderBuilder.For(employee, WorkOrderStatus.InProgress)
                .WithNumber(futureOrderNumber).DueOn(new DateOnly(2099, 12, 31)).Build());

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchOverdueOnly(true);
        specification.MatchCreator(employee);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Length.ShouldBe(1);
        orders[0].Number.ShouldBe(overdueOrderNumber);
    }

    [Test]
    public async Task ShouldReturnOnlyDueThisWeekWorkOrders_AroundTheSevenDayBoundary()
    {
        var clock = TestData.FixedClock(new DateTimeOffset(2026, 6, 15, 18, 0, 0, TimeSpan.Zero));
        var (employee, includedToday, includedLastDay) =
            await WorkOrderSpecificationSteps.SeedDueThisWeekOrdersAsync(clock);

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchDueThisWeekOnly(true);
        specification.MatchCreator(employee);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification, clock);

        orders.Length.ShouldBe(2);
        orders.Select(o => o.Number).ShouldContain(includedToday);
        orders.Select(o => o.Number).ShouldContain(includedLastDay);
    }
}
