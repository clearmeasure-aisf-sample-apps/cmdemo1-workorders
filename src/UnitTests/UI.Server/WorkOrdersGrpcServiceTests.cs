using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Server.Grpc;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Grpc.Core;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class WorkOrdersGrpcServiceTests
{
    [Test]
    public async Task Should_ReturnMappedWorkOrder_When_NumberIsFound()
    {
        var service = new WorkOrdersGrpcService(new StubBus());

        var reply = await service.GetWorkOrderByNumber(new GetWorkOrderByNumberRequest { Number = " WO-001 " }, null!);

        reply.WorkOrder.Number.ShouldBe("WO-001");
    }

    [Test]
    public async Task Should_ThrowInvalidArgument_When_NumberIsBlank()
    {
        var service = new WorkOrdersGrpcService(new StubBus());

        var error = await Should.ThrowAsync<RpcException>(() =>
            service.GetWorkOrderByNumber(new GetWorkOrderByNumberRequest { Number = " " }, null!));

        error.StatusCode.ShouldBe(StatusCode.InvalidArgument);
    }

    [Test]
    public async Task Should_ThrowNotFound_When_NumberIsUnknown()
    {
        var service = new WorkOrdersGrpcService(new MissingWorkOrderBus());

        var error = await Should.ThrowAsync<RpcException>(() =>
            service.GetWorkOrderByNumber(new GetWorkOrderByNumberRequest { Number = "WO-404" }, null!));

        error.StatusCode.ShouldBe(StatusCode.NotFound);
    }

    private sealed class MissingWorkOrderBus : StubBus
    {
        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request) =>
            request is WorkOrderByNumberQuery ? Task.FromResult(default(TResponse)!) : base.Send(request);
    }
}
