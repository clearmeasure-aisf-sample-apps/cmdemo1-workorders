using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using MediatR;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Services;

[TestFixture]
public class WorkOrderBulkActionServiceTests
{
    private static readonly Employee Creator = NewEmployee("creator", true, false);
    private static readonly Employee Fixer = NewEmployee("fixer", false, true);

    [Test]
    public async Task Assign_ValidDrafts_SendsDraftToAssignedForEach()
    {
        var bus = new StubCommandBus(Order("1", WorkOrderStatus.Draft), Order("2", WorkOrderStatus.Draft));
        var service = new WorkOrderBulkActionService(bus, new CountingUiBus());

        var outcome = await service.AssignAsync(["1", "2"], "fixer", Creator);

        outcome.ShouldBe(new BulkActionOutcome(2, 0));
        bus.Commands.ShouldAllBe(c => c is DraftToAssignedCommand && c.WorkOrder.Assignee!.UserName == "fixer");
    }

    [Test]
    public async Task Assign_NotCreator_SkipsRow()
    {
        var otherCreator = NewEmployee("someone", true, false);
        var order = Order("1", WorkOrderStatus.Draft);
        order.Creator = otherCreator;
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
        var assigned1 = Order("1", WorkOrderStatus.Assigned);
        var assigned2 = Order("2", WorkOrderStatus.Assigned);
        var draft = Order("3", WorkOrderStatus.Draft);
        var bus = new StubCommandBus(assigned1, assigned2, draft);
        var service = new WorkOrderBulkActionService(bus, new CountingUiBus());

        var outcome = await service.ChangeStatusAsync(["1", "2", "3", "missing"], AssignedToCancelledCommand.Name, Creator);

        outcome.ShouldBe(new BulkActionOutcome(2, 2));
        outcome.Text.ShouldBe("2 updated, 2 skipped");
        bus.Commands.Select(c => c.WorkOrder.Number).ShouldBe(["1", "2"]);
    }

    [Test]
    public void ChangeStatusOptions_ExcludeSaveAndAssign()
    {
        var orders = new[]
        {
            Order("1", WorkOrderStatus.Draft),
            Order("2", WorkOrderStatus.Assigned),
            Order("3", WorkOrderStatus.InProgress)
        };
        orders[2].Assignee = Creator;

        var options = WorkOrderBulkActionService.ChangeStatusOptions(orders, Creator);

        options.ShouldNotContain(SaveDraftCommand.Name);
        options.ShouldNotContain(DraftToAssignedCommand.Name);
        // #288: an In Progress order that is not waiting also offers "Wait on vendor" (not "Resume").
        options.ShouldBe([AssignedToCancelledCommand.Name, InProgressToWaitingOnVendorCommand.Name, InProgressToCompleteCommand.Name], ignoreOrder: true);
        options.ShouldNotContain(InProgressToAssignedCommand.Name);
    }

    [Test]
    public async Task ShouldNotifyWorkOrderChanged_ForEachUpdate()
    {
        var bus = new StubCommandBus(Order("1", WorkOrderStatus.Assigned), Order("2", WorkOrderStatus.Assigned));
        var uiBus = new CountingUiBus();
        var service = new WorkOrderBulkActionService(bus, uiBus);

        await service.ChangeStatusAsync(["1", "2"], AssignedToCancelledCommand.Name, Creator);

        uiBus.Events.Count.ShouldBe(2);
        uiBus.Events.ShouldAllBe(e => e is WorkOrderChangedEvent);
    }

    private static Employee NewEmployee(string userName, bool canCreate, bool canFulfill)
    {
        var employee = new Employee(userName, userName, "Person", $"{userName}@example.com") { Id = Guid.NewGuid() };
        employee.AddRole(new Role(userName + "-role", canCreate, canFulfill));
        return employee;
    }

    private static WorkOrder Order(string number, WorkOrderStatus status) =>
        new() { Id = Guid.NewGuid(), Number = number, Title = number, Status = status, Creator = Creator };

    private sealed class StubCommandBus(params WorkOrder[] orders) : Bus(null!)
    {
        public List<StateCommandBase> Commands { get; } = [];

        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            object? response = request switch
            {
                WorkOrderByNumberQuery q => orders.SingleOrDefault(o => o.Number == q.Number),
                EmployeeByUserNameQuery q => q.Username == Fixer.UserName ? Fixer : null,
                StateCommandBase command => Record(command),
                _ => throw new NotImplementedException(request.GetType().Name)
            };
            return Task.FromResult((TResponse)response!);
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
