using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
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
        var numbers = TestData.NumberSeries(13);
        var creatorUserName = await GivenLatestChangedOrdersAsync(start, numbers);

        var result = await Handle(10);

        result.Length.ShouldBe(10);
        result[0].Number.ShouldBe(numbers[11]);
        result[1].Number.ShouldBe(numbers[10]);
        result[9].Number.ShouldBe(numbers[2]);
        result.ShouldNotContain(wo => wo.Number == numbers[12]);
        result[0].Creator.ShouldNotBeNull();
        result[0].Creator!.UserName.ShouldBe(creatorUserName);
    }

    [Test]
    public async Task ShouldBreakTiesByNumber()
    {
        var created = TestData.FutureClock().GetUtcNow().UtcDateTime;
        var numbers = TestData.NumberSeries(12).Reverse().ToArray();
        await GivenTiedOrdersAsync(created, numbers);

        var first = await Handle(10);
        var second = await Handle(10);

        var expected = numbers.OrderBy(n => n, StringComparer.Ordinal).Take(10).ToArray();
        first.Select(wo => wo.Number).ShouldBe(expected);
        second.Select(wo => wo.Number).ShouldBe(expected);
    }

    private static async Task<string> GivenLatestChangedOrdersAsync(DateTime start, string[] numbers)
    {
        var userName = TestData.UserName("recentcreator");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Recent").WithLastName("Tester").WithEmail(TestData.Email(userName)).Build();
        var orders = RecentWorkOrdersSteps.BuildLatestChangedOrders(creator, start, numbers);
        await WorkOrderQuerySteps.SaveOrdersAsync(creator, orders);
        return creator.UserName;
    }

    private static async Task GivenTiedOrdersAsync(DateTime created, string[] numbers)
    {
        var userName = TestData.UserName("recenttie");
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Recent").WithLastName("Tester").WithEmail(TestData.Email(userName)).Build();
        var orders = numbers
            .Select(n => WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
                .WithNumber(n).WithTitle(n).CreatedAt(created).Build())
            .ToArray();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator, orders);
    }

    private static async Task<WorkOrder[]> Handle(int count)
    {
        var handler = new RecentWorkOrdersQueryHandler(TestHost.GetRequiredService<DataContext>());
        return await handler.Handle(new RecentWorkOrdersQuery(count));
    }
}
