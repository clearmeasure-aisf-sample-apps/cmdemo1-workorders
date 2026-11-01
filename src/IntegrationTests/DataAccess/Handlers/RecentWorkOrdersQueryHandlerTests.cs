using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class RecentWorkOrdersQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnLatestChangedFirst_LimitedToCount()
    {
        // Dates after every stored work order date make this test's rows the most recent in the database.
        var start = TestData.FutureClock().GetUtcNow().UtcDateTime;
        var creator = NewEmployee("recentcreator");
        var numbers = TestData.NumberSeries(13);
        var orders = Enumerable.Range(1, 11)
            .Select(i => Order(creator, numbers[i - 1], created: start.AddDays(10 + i)))
            .ToList();
        var oldCompletedLast = Order(creator, numbers[11], created: start, completed: start.AddDays(40));
        oldCompletedLast.Status = WorkOrderStatus.Complete;
        var undated = Order(creator, numbers[12], created: null);
        orders.Add(oldCompletedLast);
        orders.Add(undated);
        await SaveAsync(creator, orders);

        var result = await Handle(10);

        result.Length.ShouldBe(10);
        result[0].Number.ShouldBe(numbers[11]);
        result[1].Number.ShouldBe(numbers[10]);
        result[9].Number.ShouldBe(numbers[2]);
        result.ShouldNotContain(wo => wo.Number == numbers[12]);
        result[0].Creator.ShouldNotBeNull();
        result[0].Creator!.UserName.ShouldBe(creator.UserName);
    }

    [Test]
    public async Task ShouldBreakTiesByNumber()
    {
        var creator = NewEmployee("recenttie");
        var created = TestData.FutureClock().GetUtcNow().UtcDateTime;
        var numbers = TestData.NumberSeries(12).Reverse().ToArray();
        await SaveAsync(creator, numbers.Select(n => Order(creator, n, created: created)).ToList());

        var first = await Handle(10);
        var second = await Handle(10);

        var expected = numbers.OrderBy(n => n, StringComparer.Ordinal).Take(10).ToArray();
        first.Select(wo => wo.Number).ShouldBe(expected);
        second.Select(wo => wo.Number).ShouldBe(expected);
    }

    private static Employee NewEmployee(string prefix)
    {
        var userName = TestData.UserName(prefix);
        return new Employee(userName, "Recent", "Tester", TestData.Email(userName));
    }

    private static async Task<WorkOrder[]> Handle(int count)
    {
        var handler = new RecentWorkOrdersQueryHandler(TestHost.GetRequiredService<DataContext>());
        return await handler.Handle(new RecentWorkOrdersQuery(count));
    }

    private static WorkOrder Order(Employee creator, string number, DateTime? created,
        DateTime? completed = null) =>
        new()
        {
            Creator = creator, Number = number, Title = number, Status = WorkOrderStatus.Draft,
            CreatedDate = created, AssignedDate = null, CompletedDate = completed
        };

    private static async Task SaveAsync(Employee creator, List<WorkOrder> orders)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.AddRange(orders);
        await context.SaveChangesAsync();
    }
}
