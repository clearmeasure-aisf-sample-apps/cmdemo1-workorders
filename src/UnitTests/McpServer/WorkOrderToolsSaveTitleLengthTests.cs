using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.McpServer.Tools;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class WorkOrderToolsSaveTitleLengthTests
{
    [Test]
    public async Task SaveWorkOrder_TitleOver300_ReturnsLengthMessageWithoutSaving()
    {
        var bus = new StubWorkOrderToolsBus();

        var result = await WorkOrderTools.SaveWorkOrder(bus, "WO-001", "creator1",
            title: new string('T', WorkOrder.TitleMaxLength + 1));

        result.ShouldBe("Title cannot exceed 300 characters.");
        bus.SavedCommands.ShouldBeEmpty();
    }

    [Test]
    public async Task SaveWorkOrder_TitleAt300_SendsSaveDraft()
    {
        var bus = new StubWorkOrderToolsBus();

        await WorkOrderTools.SaveWorkOrder(bus, "WO-001", "creator1",
            title: new string('T', WorkOrder.TitleMaxLength));

        bus.SavedCommands.Count.ShouldBe(1);
        bus.SavedCommands[0].WorkOrder.Title!.Length.ShouldBe(WorkOrder.TitleMaxLength);
    }

    private class StubWorkOrderToolsBus : IBus
    {
        private readonly Employee _creator = new("creator1", "Creator", "One", "creator1@example.com");
        private readonly WorkOrder _workOrder;

        public StubWorkOrderToolsBus()
        {
            _workOrder = new WorkOrder
            {
                Number = "WO-001",
                Title = "Original title",
                Description = "Original description",
                Status = WorkOrderStatus.Draft,
                Creator = _creator
            };
        }

        public List<SaveDraftCommand> SavedCommands { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            object response = request switch
            {
                WorkOrderByNumberQuery => _workOrder,
                EmployeeByUserNameQuery => _creator,
                SaveDraftCommand command => Record(command),
                _ => throw new NotSupportedException(request.GetType().Name)
            };
            return Task.FromResult((TResponse)response);
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => Task.CompletedTask;

        private StateCommandResult Record(SaveDraftCommand command)
        {
            SavedCommands.Add(command);
            return new StateCommandResult(command.WorkOrder);
        }
    }
}
