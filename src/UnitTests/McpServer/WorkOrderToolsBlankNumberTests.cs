using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Mediator;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class WorkOrderToolsBlankNumberTests
{
    [Test]
    public async Task GetWorkOrder_WhenNumberIsBlank_ReturnsNotFoundWithoutSending()
    {
        var bus = new StubRecordingBus();

        var result = await WorkOrderTools.GetWorkOrder(bus, "  ");

        result.ShouldBe("No work order found with number '  '.");
        bus.Sent.ShouldBeEmpty();
    }

    [Test]
    public async Task ListWorkOrderAttachments_WhenNumberIsBlank_ReturnsNotFoundWithoutSending()
    {
        var bus = new StubRecordingBus();

        var result = await WorkOrderTools.ListWorkOrderAttachments(bus, "");

        result.ShouldBe("No work order found with number ''.");
        bus.Sent.ShouldBeEmpty();
    }

    [Test]
    public async Task GetWorkOrder_WhenNumberIsKnown_ReturnsDetailJson()
    {
        var bus = new StubRecordingBus();

        var result = await WorkOrderTools.GetWorkOrder(bus, "WO-001");

        result.ShouldContain("WO-001");
        bus.Sent.ShouldContain(request => request is WorkOrderByNumberQuery);
    }

    [Test]
    public async Task ListWorkOrderAttachments_WhenNumberIsKnown_ReturnsAttachmentsJson()
    {
        var bus = new StubRecordingBus();

        var result = await WorkOrderTools.ListWorkOrderAttachments(bus, "WO-001");

        result.ShouldNotBeNull();
        bus.Sent.ShouldContain(request => request is WorkOrderByNumberQuery);
        bus.Sent.ShouldContain(request => request is WorkOrderAttachmentsQuery);
    }

    private sealed class StubRecordingBus : IBus
    {
        private readonly WorkOrder _workOrder = WorkOrderBuilder.Create()
            .WithNumber("WO-001")
            .WithTitle("Lawn")
            .WithStatus(WorkOrderStatus.Draft)
            .CreatedBy(EmployeeBuilder.Create("creator1")
                .WithFirstName("Creator")
                .WithLastName("One")
                .WithEmail("c@example.com")
                .Build())
            .Build();


        public List<object> Sent { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            Sent.Add(request);
            object response = request switch
            {
                WorkOrderByNumberQuery => _workOrder,
                WorkOrderAttachmentsQuery => Array.Empty<WorkOrderAttachment>(),
                _ => throw new NotSupportedException(request.GetType().Name)
            };
            return Task.FromResult((TResponse)response);
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => Task.CompletedTask;
    }
}
