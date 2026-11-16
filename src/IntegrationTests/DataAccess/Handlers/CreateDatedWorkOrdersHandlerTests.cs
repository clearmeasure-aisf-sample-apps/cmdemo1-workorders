using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class CreateDatedWorkOrdersHandlerTests
{
    private sealed class StubFailingPublishBus : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request) =>
            throw new NotSupportedException();

        public Task<object?> Send(object request) =>
            throw new NotSupportedException();

        public Task Publish(INotification notification) =>
            Task.FromException(new InvalidOperationException("Simulated telemetry failure"));
    }

    private sealed class ThrowingAfterCountNumberGenerator(int succeedCount) : IWorkOrderNumberGenerator
    {
        private int _count;

        public string GenerateNumber()
        {
            _count++;
            if (_count > succeedCount)
            {
                throw new InvalidOperationException("Simulated failure mid-batch");
            }

            return Guid.NewGuid().ToString("N")[..7].ToUpperInvariant();
        }
    }

    [Test]
    public async Task Handle_ShouldPersistNullAndDateWithoutUtcShift()
    {
        var withDateNumber = TestData.Number("D");
        var withoutDateNumber = TestData.Number("D");
        await GivenDueDatePairAsync(withDateNumber, withoutDateNumber);

        await ThenDueDatesUnshiftedAsync(withDateNumber, withoutDateNumber);
    }

    [Test]
    public async Task Handle_ShouldCreateTenInOneTransaction()
    {
        var creatorUserName = TestData.UserName("tl");
        var assigneeUserName = TestData.UserName("gw");
        await GivenCreatorAndAssigneeAsync(creatorUserName, assigneeUserName);
        var dueDates = WeeklyDueDates(10);

        var result = await TestHost.GetRequiredService<IBus>().Send(new CreateDatedWorkOrdersCommand(
            creatorUserName, assigneeUserName, "Mow the grass", "Weekly Saturday mow", dueDates));

        await ThenTenAssignedAsync(result, creatorUserName, assigneeUserName, dueDates);
    }

    [Test]
    public async Task Handle_WhenAssigneeMissing_CreatesZero()
    {
        var creatorUserName = TestData.UserName("tl");
        var missingAssigneeUserName = TestData.UserName("missing");

        var creator = new Employee(creatorUserName, "Dated", "Creator", "t@test.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await bus.Send(new CreateDatedWorkOrdersCommand(
            creatorUserName,
            missingAssigneeUserName,
            "Mow the grass",
            "Weekly Saturday mow",
            [new DateOnly(2026, 8, 29)]));

        result.Success.ShouldBeFalse();
        result.WorkOrders.Count.ShouldBe(0);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            (await context.Set<WorkOrder>().CountAsync(w => w.Creator!.UserName == creatorUserName)).ShouldBe(0);
        }
    }

    [Test]
    public async Task Handle_WhenDueDatesEmpty_CreatesZero()
    {
        var creatorUserName = TestData.UserName("tl");
        var assigneeUserName = TestData.UserName("gw");

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await bus.Send(new CreateDatedWorkOrdersCommand(
            creatorUserName,
            assigneeUserName,
            "Mow the grass",
            "Weekly Saturday mow",
            []));

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("At least one due date");
        result.WorkOrders.Count.ShouldBe(0);
    }

    [Test]
    public async Task Handle_WhenBatchExceedsLimit_CreatesZero()
    {
        var creatorUserName = TestData.UserName("tl");
        var assigneeUserName = TestData.UserName("gw");

        var dueDates = Enumerable.Range(0, CreateDatedWorkOrdersCommand.MaximumBatchSize + 1)
            .Select(i => new DateOnly(2026, 8, 29).AddDays(i))
            .ToList();
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await bus.Send(new CreateDatedWorkOrdersCommand(
            creatorUserName,
            assigneeUserName,
            "Mow the grass",
            "Weekly Saturday mow",
            dueDates));

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("cannot exceed 10");
        result.WorkOrders.Count.ShouldBe(0);
        await using var context = TestHost.GetRequiredService<DbContext>();
        (await context.Set<WorkOrder>().CountAsync(w => w.Creator!.UserName == creatorUserName || w.Assignee!.UserName == assigneeUserName)).ShouldBe(0);
    }

    [Test]
    public async Task Handle_WhenCreatorMissing_CreatesZero()
    {
        var assigneeUserName = TestData.UserName("gw");
        var missingCreatorUserName = TestData.UserName("missing");

        var assignee = new Employee(assigneeUserName, "Dated", "Assignee", "w@test.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(assignee);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await bus.Send(new CreateDatedWorkOrdersCommand(
            missingCreatorUserName,
            assigneeUserName,
            "Mow the grass",
            "Weekly Saturday mow",
            [new DateOnly(2026, 8, 29)]));

        result.Success.ShouldBeFalse();
        result.WorkOrders.Count.ShouldBe(0);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            (await context.Set<WorkOrder>().CountAsync(w => w.Assignee!.UserName == assigneeUserName)).ShouldBe(0);
        }
    }

    [Test]
    public async Task Handle_WhenPartialBatchFails_RollsBackAll()
    {
        var creatorUserName = TestData.UserName("tl");
        var assigneeUserName = TestData.UserName("gw");
        await GivenCreatorAndAssigneeAsync(creatorUserName, assigneeUserName);

        await Should.ThrowAsync<InvalidOperationException>(async () => await
            CreateHandler(new ThrowingAfterCountNumberGenerator(5), TestHost.GetRequiredService<IBus>())
                .Handle(MowCommand(creatorUserName, assigneeUserName, WeeklyDueDates(10)), CancellationToken.None));

        await ThenAssigneeHasCountAsync(assigneeUserName, 0);
    }

    [Test]
    public async Task Handle_WhenTelemetryPublishFails_ReturnsCommittedWorkOrders()
    {
        var creatorUserName = TestData.UserName("tl");
        var assigneeUserName = TestData.UserName("gw");
        await GivenCreatorAndAssigneeAsync(creatorUserName, assigneeUserName);

        var result = await CreateHandler(new ThrowingAfterCountNumberGenerator(1), new StubFailingPublishBus())
            .Handle(MowCommand(creatorUserName, assigneeUserName, [new DateOnly(2026, 8, 29)]), CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.WorkOrders.Count.ShouldBe(1);
        await ThenAssigneeHasCountAsync(assigneeUserName, 1);
    }

    private static async Task GivenDueDatePairAsync(string withDateNumber, string withoutDateNumber)
    {
        var creator = EmployeeBuilder.Create(TestData.UserName("creator1"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john@example.com").Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).WithNumber(withDateNumber)
                .WithTitle("Has due date").WithDescription("desc").DueOn(new DateOnly(2026, 8, 29)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).WithNumber(withoutDateNumber)
                .WithTitle("No due date").WithDescription("desc").Build());
    }

    private static async Task GivenCreatorAndAssigneeAsync(string creatorUserName, string assigneeUserName)
    {
        var creator = EmployeeBuilder.Create(creatorUserName)
            .WithFirstName("Dated").WithLastName("Creator").WithEmail("t@test.com").Build();
        var assignee = EmployeeBuilder.Create(assigneeUserName)
            .WithFirstName("Dated").WithLastName("Assignee").WithEmail("w@test.com").Build();
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.AddRange(creator, assignee);
        await context.SaveChangesAsync();
    }

    private static List<DateOnly> WeeklyDueDates(int count) =>
        Enumerable.Range(0, count).Select(i => new DateOnly(2026, 8, 29).AddDays(7 * i)).ToList();

    private static CreateDatedWorkOrdersCommand MowCommand(
        string creatorUserName, string assigneeUserName, IReadOnlyList<DateOnly> dueDates) =>
        new(creatorUserName, assigneeUserName, "Mow the grass", "Weekly Saturday mow", dueDates);

    private static CreateDatedWorkOrdersHandler CreateHandler(IWorkOrderNumberGenerator numbers, IBus bus) =>
        new(TestHost.GetRequiredService<DbContext>(), bus, numbers, TimeProvider.System,
            NullLogger<CreateDatedWorkOrdersHandler>.Instance);

    private static async Task ThenDueDatesUnshiftedAsync(string withDateNumber, string withoutDateNumber)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        var loadedWith = await context.Set<WorkOrder>().SingleAsync(w => w.Number == withDateNumber);
        var loadedWithout = await context.Set<WorkOrder>().SingleAsync(w => w.Number == withoutDateNumber);
        loadedWith.DueDate.ShouldBe(new DateOnly(2026, 8, 29));
        loadedWithout.DueDate.ShouldBeNull();
    }

    private static async Task ThenTenAssignedAsync(
        CreateDatedWorkOrdersResult result, string creatorUserName, string assigneeUserName, List<DateOnly> dueDates)
    {
        result.Success.ShouldBeTrue();
        result.WorkOrders.Count.ShouldBe(10);
        await using var context = TestHost.GetRequiredService<DbContext>();
        var created = await context.Set<WorkOrder>()
            .Where(w => w.Assignee!.UserName == assigneeUserName)
            .OrderBy(w => w.DueDate).ToListAsync();
        created.Count.ShouldBe(10);
        created.Select(w => w.DueDate!.Value).ToList().ShouldBe(dueDates);
        created.ShouldAllBe(w => w.Status == WorkOrderStatus.Assigned);
        created.ShouldAllBe(w => w.Creator!.UserName == creatorUserName);
    }

    private static async Task ThenAssigneeHasCountAsync(string assigneeUserName, int expected)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        (await context.Set<WorkOrder>().CountAsync(w => w.Assignee!.UserName == assigneeUserName)).ShouldBe(expected);
    }
}
