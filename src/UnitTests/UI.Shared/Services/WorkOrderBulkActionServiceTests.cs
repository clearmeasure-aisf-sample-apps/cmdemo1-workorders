using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Mediator;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Services;

[TestFixture]
public class WorkOrderBulkActionServiceTests
{
    private static readonly Employee Creator = EmployeeBuilder.Create("creator")
        .WithFirstName("creator").WithLastName("Person").WithEmail("creator@example.com")
        .WithId(Guid.NewGuid()).WithRole("creator-role", canCreate: true, canFulfill: false).Build();

    private static readonly Employee Fixer = EmployeeBuilder.Create("fixer")
        .WithFirstName("fixer").WithLastName("Person").WithEmail("fixer@example.com")
        .WithId(Guid.NewGuid()).WithRole("fixer-role", canCreate: false, canFulfill: true).Build();

    [Test]
    public async Task Assign_ValidDrafts_SendsDraftToAssignedForEach()
    {
        var bus = new StubCommandBus(
            WorkOrderBuilder.For(Creator, WorkOrderStatus.Draft).WithId(Guid.NewGuid()).WithNumber("1").WithTitle("1").Build(),
            WorkOrderBuilder.For(Creator, WorkOrderStatus.Draft).WithId(Guid.NewGuid()).WithNumber("2").WithTitle("2").Build());
        var service = new WorkOrderBulkActionService(bus, new CountingUiBus());

        var outcome = await service.AssignAsync(["1", "2"], "fixer", Creator);

        outcome.ShouldBe(new BulkActionOutcome(2, 0));
        bus.BatchedReads.ShouldBe(1);
        bus.ByNumberReads.ShouldBe(0);
        bus.Commands.ShouldAllBe(c => c is DraftToAssignedCommand && c.WorkOrder.Assignee!.UserName == "fixer");
    }

    [Test]
    public async Task Assign_NotCreator_SkipsRow()
    {
        var otherCreator = EmployeeBuilder.Create("someone")
            .WithFirstName("someone").WithLastName("Person").WithEmail("someone@example.com")
            .WithId(Guid.NewGuid()).WithRole("someone-role", canCreate: true, canFulfill: false).Build();
        var order = WorkOrderBuilder.For(otherCreator, WorkOrderStatus.Draft)
            .WithId(Guid.NewGuid()).WithNumber("1").WithTitle("1").Build();
        var bus = new StubCommandBus(order);
        var service = new WorkOrderBulkActionService(bus, new CountingUiBus());

        var outcome = await service.AssignAsync(["1"], "fixer", Creator);

        outcome.ShouldBe(new BulkActionOutcome(0, 1));
        outcome.Text.ShouldBe("0 updated, 1 skipped");
        bus.Commands.ShouldBeEmpty();
    }

    [Test]
    public async Task ChangeStatus_Mixed_ReportsUpdatedAndSkipped()
    {
        var bus = new StubCommandBus(
            WorkOrderBuilder.For(Creator, WorkOrderStatus.Assigned).WithId(Guid.NewGuid()).WithNumber("1").WithTitle("1").Build(),
            WorkOrderBuilder.For(Creator, WorkOrderStatus.Assigned).WithId(Guid.NewGuid()).WithNumber("2").WithTitle("2").Build(),
            WorkOrderBuilder.For(Creator, WorkOrderStatus.Draft).WithId(Guid.NewGuid()).WithNumber("3").WithTitle("3").Build());
        var service = new WorkOrderBulkActionService(bus, new CountingUiBus());

        var outcome = await service.ChangeStatusAsync(["1", "2", "3", "missing"], AssignedToCancelledCommand.Name, Creator);

        outcome.ShouldBe(new BulkActionOutcome(2, 2));
        outcome.Text.ShouldBe("2 updated, 2 skipped");
        bus.BatchedReads.ShouldBe(1);
        bus.ByNumberReads.ShouldBe(0);
        bus.Commands.Select(c => c.WorkOrder.Number).ShouldBe(["1", "2"]);
    }

    [Test]
    public void ChangeStatusOptions_ExcludeSaveAndAssign()
    {
        var inProgress = WorkOrderBuilder.For(Creator, WorkOrderStatus.InProgress)
            .WithId(Guid.NewGuid()).WithNumber("3").WithTitle("3").AssignedTo(Creator).Build();
        var orders = new[]
        {
            WorkOrderBuilder.For(Creator, WorkOrderStatus.Draft).WithId(Guid.NewGuid()).WithNumber("1").WithTitle("1").Build(),
            WorkOrderBuilder.For(Creator, WorkOrderStatus.Assigned).WithId(Guid.NewGuid()).WithNumber("2").WithTitle("2").Build(),
            inProgress
        };

        var options = WorkOrderBulkActionService.ChangeStatusOptions(orders, Creator);

        options.ShouldNotContain(SaveDraftCommand.Name);
        options.ShouldNotContain(DraftToAssignedCommand.Name);
        // #288: an In Progress order that is not waiting also offers "Wait on vendor" (not "Resume").
        options.ShouldBe([AssignedToCancelledCommand.Name, InProgressToWaitingOnVendorCommand.Name, InProgressToCompleteCommand.Name], ignoreOrder: true);
        options.ShouldNotContain(InProgressToAssignedCommand.Name);
    }

    [Test]
    public async Task ChangeStatus_NoNumbers_DoesNotRead()
    {
        var bus = new StubCommandBus();
        var service = new WorkOrderBulkActionService(bus, new CountingUiBus());

        var outcome = await service.ChangeStatusAsync([], AssignedToCancelledCommand.Name, Creator);

        outcome.ShouldBe(new BulkActionOutcome(0, 0));
        bus.BatchedReads.ShouldBe(0);
        bus.Commands.ShouldBeEmpty();
    }

    [Test]
    public async Task ChangeStatus_BlankNumbers_SkipsWithoutACommand()
    {
        var bus = new StubCommandBus(
            WorkOrderBuilder.For(Creator, WorkOrderStatus.Assigned).WithId(Guid.NewGuid()).WithNumber("1").WithTitle("1").Build());
        var service = new WorkOrderBulkActionService(bus, new CountingUiBus());

        var outcome = await service.ChangeStatusAsync(["", " "], AssignedToCancelledCommand.Name, Creator);

        outcome.ShouldBe(new BulkActionOutcome(0, 2));
        bus.BatchedReads.ShouldBe(1);
        bus.ByNumberReads.ShouldBe(0);
        bus.Commands.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldNotifyWorkOrderChanged_ForEachUpdate()
    {
        var bus = new StubCommandBus(
            WorkOrderBuilder.For(Creator, WorkOrderStatus.Assigned).WithId(Guid.NewGuid()).WithNumber("1").WithTitle("1").Build(),
            WorkOrderBuilder.For(Creator, WorkOrderStatus.Assigned).WithId(Guid.NewGuid()).WithNumber("2").WithTitle("2").Build());
        var uiBus = new CountingUiBus();
        var service = new WorkOrderBulkActionService(bus, uiBus);

        await service.ChangeStatusAsync(["1", "2"], AssignedToCancelledCommand.Name, Creator);

        uiBus.Events.Count.ShouldBe(2);
        uiBus.Events.ShouldAllBe(e => e is WorkOrderChangedEvent);
    }

    private sealed class StubCommandBus(params WorkOrder[] orders) : Bus(null!)
    {
        public List<StateCommandBase> Commands { get; } = [];

        public int BatchedReads { get; private set; }

        public int ByNumberReads { get; private set; }

        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            object? response = request switch
            {
                WorkOrdersByNumbersQuery query => LoadBatch(query),
                WorkOrderByNumberQuery query => LoadOne(query),
                EmployeeByUserNameQuery query => query.Username == Fixer.UserName ? Fixer : null,
                StateCommandBase command => Record(command),
                _ => throw new NotImplementedException(request.GetType().Name)
            };
            return Task.FromResult((TResponse)response!);
        }

        private WorkOrder[] LoadBatch(WorkOrdersByNumbersQuery query)
        {
            BatchedReads++;
            var wanted = new HashSet<string>(query.Numbers, StringComparer.Ordinal);
            return orders.Where(order => order.Number != null && wanted.Contains(order.Number)).ToArray();
        }

        private WorkOrder? LoadOne(WorkOrderByNumberQuery query)
        {
            ByNumberReads++;
            return orders.SingleOrDefault(order => order.Number == query.Number);
        }

        private StateCommandResult Record(StateCommandBase command)
        {
            Commands.Add(command);
            return new StateCommandResult(command.WorkOrder, command.TransitionVerbPresentTense, "done");
        }
    }

    private sealed class CountingUiBus : IUiBus
    {
        public List<object> Events { get; } = [];

        public void Register(IListener listener)
        {
        }

        public void UnRegister(IListener listener)
        {
        }

        public IListener<T>[] GetListeners<T>() where T : IUiBusEvent => [];

        public void Notify<T>(T eventObject) where T : IUiBusEvent => Events.Add(eventObject);

        public void UnRegisterAll()
        {
        }
    }
}
