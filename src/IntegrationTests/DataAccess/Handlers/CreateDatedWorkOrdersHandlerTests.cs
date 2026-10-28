using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using MediatR;
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
        var creatorUserName = TestData.UserName("creator1");
        var withDateNumber = TestData.Number("D");
        var withoutDateNumber = TestData.Number("D");

        var creator = new Employee(creatorUserName, "John", "Doe", "john@example.com");
        var withDate = new WorkOrder
        {
            Number = withDateNumber,
            Title = "Has due date",
            Description = "desc",
            Creator = creator,
            Status = WorkOrderStatus.Draft,
            DueDate = new DateOnly(2026, 8, 29)
        };
        var withoutDate = new WorkOrder
        {
            Number = withoutDateNumber,
            Title = "No due date",
            Description = "desc",
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(withDate);
            context.Add(withoutDate);
            await context.SaveChangesAsync();
        }

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var loadedWith = await context.Set<WorkOrder>().SingleAsync(w => w.Number == withDateNumber);
            var loadedWithout = await context.Set<WorkOrder>().SingleAsync(w => w.Number == withoutDateNumber);

            loadedWith.DueDate.ShouldBe(new DateOnly(2026, 8, 29));
            loadedWithout.DueDate.ShouldBeNull();
        }
    }

    [Test]
    public async Task Handle_ShouldCreateTenInOneTransaction()
    {
        var creatorUserName = TestData.UserName("tl");
        var assigneeUserName = TestData.UserName("gw");

        var creator = new Employee(creatorUserName, "Dated", "Creator", "t@test.com");
        var assignee = new Employee(assigneeUserName, "Dated", "Assignee", "w@test.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            await context.SaveChangesAsync();
        }

        var dueDates = Enumerable.Range(0, 10)
            .Select(i => new DateOnly(2026, 8, 29).AddDays(7 * i))
            .ToList();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await bus.Send(new CreateDatedWorkOrdersCommand(
            creatorUserName,
            assigneeUserName,
            "Mow the grass",
            "Weekly Saturday mow",
            dueDates));

        result.Success.ShouldBeTrue();
        result.WorkOrders.Count.ShouldBe(10);

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var created = await context.Set<WorkOrder>()
                .Where(w => w.Assignee!.UserName == assigneeUserName)
                .OrderBy(w => w.DueDate)
                .ToListAsync();
            created.Count.ShouldBe(10);
            created.Select(w => w.DueDate!.Value).ToList().ShouldBe(dueDates);
            created.ShouldAllBe(w => w.Status == WorkOrderStatus.Assigned);
            created.ShouldAllBe(w => w.Creator!.UserName == creatorUserName);
        }
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

        var creator = new Employee(creatorUserName, "Dated", "Creator", "t@test.com");
        var assignee = new Employee(assigneeUserName, "Dated", "Assignee", "w@test.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            await context.SaveChangesAsync();
        }

        var dueDates = Enumerable.Range(0, 10)
            .Select(i => new DateOnly(2026, 8, 29).AddDays(7 * i))
            .ToList();

        await using var db = TestHost.GetRequiredService<DbContext>();
        var bus = TestHost.GetRequiredService<IBus>();
        var handler = new CreateDatedWorkOrdersHandler(
            db,
            bus,
            new ThrowingAfterCountNumberGenerator(5),
            TimeProvider.System,
            NullLogger<CreateDatedWorkOrdersHandler>.Instance);

        await Should.ThrowAsync<InvalidOperationException>(() => handler.Handle(
            new CreateDatedWorkOrdersCommand(
                creatorUserName,
                assigneeUserName,
                "Mow the grass",
                "Weekly Saturday mow",
                dueDates),
            CancellationToken.None));

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            (await context.Set<WorkOrder>().CountAsync(w => w.Assignee!.UserName == assigneeUserName)).ShouldBe(0);
        }
    }

    [Test]
    public async Task Handle_WhenTelemetryPublishFails_ReturnsCommittedWorkOrders()
    {
        var creatorUserName = TestData.UserName("tl");
        var assigneeUserName = TestData.UserName("gw");

        var creator = new Employee(creatorUserName, "Dated", "Creator", "t@test.com");
        var assignee = new Employee(assigneeUserName, "Dated", "Assignee", "w@test.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            await context.SaveChangesAsync();
        }

        await using var db = TestHost.GetRequiredService<DbContext>();
        var handler = new CreateDatedWorkOrdersHandler(
            db,
            new StubFailingPublishBus(),
            new ThrowingAfterCountNumberGenerator(1),
            TimeProvider.System,
            NullLogger<CreateDatedWorkOrdersHandler>.Instance);

        var result = await handler.Handle(
            new CreateDatedWorkOrdersCommand(
                creatorUserName,
                assigneeUserName,
                "Mow the grass",
                "Weekly Saturday mow",
                [new DateOnly(2026, 8, 29)]),
            CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.WorkOrders.Count.ShouldBe(1);
        await using var verifyContext = TestHost.GetRequiredService<DbContext>();
        (await verifyContext.Set<WorkOrder>().CountAsync(w => w.Assignee!.UserName == assigneeUserName)).ShouldBe(1);
    }
}
