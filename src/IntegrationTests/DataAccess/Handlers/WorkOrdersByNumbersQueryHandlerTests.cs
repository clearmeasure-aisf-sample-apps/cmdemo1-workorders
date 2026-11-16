using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrdersByNumbersQueryHandlerTests
{
    [Test]
    public async Task ShouldLoadEveryTargetedWorkOrderInOneQuery()
    {
        var userName = TestData.UserName("batch");
        var numbers = TestData.NumberSeries(8);
        var context = await GivenBatchOrdersAsync(userName, numbers);
        var handler = new WorkOrdersByNumbersQueryHandler(context);

        await ThenOneAndAllShareCommandCountAsync(handler, context, userName, numbers);
        await ThenEmptyAndTooManyStayAtZeroAsync(handler, context);
    }

    private static async Task<CountingDataContext> GivenBatchOrdersAsync(string userName, string[] numbers)
    {
        var creator = EmployeeBuilder.Create(userName)
            .WithFirstName("Batch").WithLastName("Reader").WithEmail(TestData.Email(userName)).Build();
        var orders = numbers.Select(number => WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(number).WithTitle(number).Build()).ToArray();
        var context = new CountingDataContext(TestHost.GetRequiredService<IDatabaseConfiguration>());
        context.Add(creator);
        context.AddRange(orders.AsEnumerable());
        await context.SaveChangesAsync();
        return context;
    }

    private static async Task ThenOneAndAllShareCommandCountAsync(
        WorkOrdersByNumbersQueryHandler handler, CountingDataContext context, string userName, string[] numbers)
    {
        context.Commands.Reset();
        var one = await handler.Handle(new WorkOrdersByNumbersQuery([numbers[0]]));
        var oneCommands = context.Commands.ReaderCommands;
        context.Commands.Reset();
        var all = await handler.Handle(new WorkOrdersByNumbersQuery([.. numbers, numbers[0], ""]));
        var allCommands = context.Commands.ReaderCommands;
        one.Length.ShouldBe(1);
        one[0].Number.ShouldBe(numbers[0]);
        one[0].Creator!.UserName.ShouldBe(userName);
        all.Select(order => order.Number).OrderBy(number => number, StringComparer.Ordinal)
            .ShouldBe(numbers.OrderBy(number => number, StringComparer.Ordinal));
        oneCommands.ShouldBe(allCommands);
        oneCommands.ShouldBe(1, string.Join(Environment.NewLine, context.Commands.Texts));
    }

    private static async Task ThenEmptyAndTooManyStayAtZeroAsync(
        WorkOrdersByNumbersQueryHandler handler, CountingDataContext context)
    {
        context.Commands.Reset();
        (await handler.Handle(new WorkOrdersByNumbersQuery([]))).ShouldBeEmpty();
        (await handler.Handle(new WorkOrdersByNumbersQuery(["", " "]))).ShouldBeEmpty();
        context.Commands.ReaderCommands.ShouldBe(0);
        var tooMany = Enumerable.Range(0, WorkOrdersByNumbersQuery.MaxNumbers + 1)
            .Select(index => index.ToString()).ToArray();
        await Should.ThrowAsync<InvalidOperationException>(() =>
            handler.Handle(new WorkOrdersByNumbersQuery(tooMany)));
        context.Commands.ReaderCommands.ShouldBe(0);
    }
}
