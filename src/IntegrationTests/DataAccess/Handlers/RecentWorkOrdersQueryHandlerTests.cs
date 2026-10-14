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
    private static readonly DateTime Start = new(2026, 9, 1, 8, 0, 0);

    [Test]
    public async Task ShouldReturnLatestChangedFirst_LimitedToCount()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("recentcreator", "Recent", "Creator", "rc@example.com");
        var orders = Enumerable.Range(1, 11)
            .Select(i => Order(creator, $"R{i:000000}", created: Start.AddDays(10 + i)))
            .ToList();
        var oldCompletedLast = Order(creator, "R000099", created: Start, completed: Start.AddDays(40));
        oldCompletedLast.Status = WorkOrderStatus.Complete;
        var undated = Order(creator, "R000100", created: null);
        orders.Add(oldCompletedLast);
        orders.Add(undated);
        await SaveAsync(creator, orders);

        var result = await Handle(10);

        result.Length.ShouldBe(10);
        result[0].Number.ShouldBe("R000099");
        result[1].Number.ShouldBe("R000011");
        result[9].Number.ShouldBe("R000003");
        result.ShouldNotContain(wo => wo.Number == "R000100");
        result[0].Creator.ShouldNotBeNull();
        result[0].Creator!.UserName.ShouldBe("recentcreator");
    }

    [Test]
    public async Task ShouldBreakTiesByNumber()
    {
        new DatabaseTests().Clean();
        var creator = new Employee("recenttie", "Recent", "Tie", "rt@example.com");
        var created = new DateTime(2026, 10, 1, 9, 30, 0);
        var numbers = Enumerable.Range(1, 12).Select(i => $"T{i:X6}").Reverse().ToArray();
        await SaveAsync(creator, numbers.Select(n => Order(creator, n, created: created)).ToList());

        var first = await Handle(10);
        var second = await Handle(10);

        var expected = numbers.OrderBy(n => n, StringComparer.Ordinal).Take(10).ToArray();
        first.Select(wo => wo.Number).ShouldBe(expected);
        second.Select(wo => wo.Number).ShouldBe(expected);
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
        context.AddRange(orders.Cast<object>());
        await context.SaveChangesAsync();
    }
}
