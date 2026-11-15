using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Server;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class AutoReformatAgentServiceTests
{
    [Test]
    public void IsDisabled_ReturnsTrue_WhenConfigurationTrue()
    {
        var config = ConfigurationWithDisable("true");

        AutoReformatAgentService.IsDisabled(config).ShouldBeTrue();
    }

    [Test]
    public void IsDisabled_ReturnsFalse_WhenConfigurationMissing()
    {
        AutoReformatAgentService.IsDisabled(new ConfigurationBuilder().Build()).ShouldBeFalse();
    }

    [Test]
    public async Task LoadDraftWorkOrdersAsync_ReturnsDraftOrders()
    {
        var workOrder = DraftWorkOrder("WO-1");
        var bus = new StubDraftWorkOrderBus([workOrder]);

        var results = await AutoReformatAgentService.LoadDraftWorkOrdersAsync(bus);

        results.Length.ShouldBe(1);
        results[0].Number.ShouldBe("WO-1");
    }

    [Test]
    public async Task ReformatWorkOrdersAsync_SendsCommand_WhenAgentReturnsReformat()
    {
        var workOrder = DraftWorkOrder("WO-10", "old title", "old description");
        var bus = new StubRecordingBus([workOrder]);
        var service = CreateService(bus, new StubReformatAgent(new WorkOrderReformatAgent.ReformatResult("New Title", "New description.")));

        await service.ReformatWorkOrdersAsync();

        var command = bus.LastCommand.ShouldBeOfType<ReformatWorkOrderCommand>();
        command.Number.ShouldBe("WO-10");
        command.Title.ShouldBe("New Title");
        command.Description.ShouldBe("New description.");
    }

    [Test]
    public async Task ReformatWorkOrdersAsync_SavesNothing_WhenAgentReturnsNoChange()
    {
        var workOrder = DraftWorkOrder("WO-11", "Same", "Same");
        var bus = new StubRecordingBus([workOrder]);
        var service = CreateService(bus, new StubReformatAgent(result: null));

        await service.ReformatWorkOrdersAsync();

        bus.LastCommand.ShouldBeNull();
    }

    [Test]
    public async Task ReformatWorkOrdersAsync_SavesNothingAndLogsOnce_WhenAgentThrows()
    {
        var workOrder = DraftWorkOrder("WO-12", "t", "d");
        var bus = new StubRecordingBus([workOrder]);
        var logger = new CapturingLogger<AutoReformatAgentService>();
        var service = CreateService(
            bus,
            new StubReformatAgent(exception: new InvalidOperationException("model-failed")),
            logger);

        await service.ReformatWorkOrdersAsync();

        bus.LastCommand.ShouldBeNull();
        logger.Inner.ErrorCount.ShouldBe(1);
        logger.Inner.MessagesAt(LogLevel.Error).ShouldHaveSingleItem().ShouldContain("WO-12");
    }

    [Test]
    public async Task ExecuteAsync_ReturnsImmediately_WhenDisabled()
    {
        var service = CreateService(
            new StubDraftWorkOrderBus([]),
            new StubReformatAgent(result: null),
            options: new AutoReformatOptions(disabled: true, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30)));

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task ExecuteAsync_ExitsLoop_WhenCancelledDuringDelay()
    {
        var service = CreateService(new StubDraftWorkOrderBus([]), new StubReformatAgent(result: null));

        using var cts = new CancellationTokenSource();
        var start = service.StartAsync(cts.Token);
        await Task.Delay(50, CancellationToken.None);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);
        await start;
    }

    [Test]
    public async Task ExecuteAsync_Continues_WhenReformatThrowsThenCancelled()
    {
        var service = CreateService(new StubThrowingBus(), new StubReformatAgent(result: null));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await service.StartAsync(cts.Token);
        await Task.Delay(50, CancellationToken.None);
        await service.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task ReformatWorkOrdersAsync_DoesNotThrow_WhenNoDraftOrders()
    {
        var service = CreateService(new StubDraftWorkOrderBus([]), new StubReformatAgent(result: null));

        await service.ReformatWorkOrdersAsync();
    }

    private static AutoReformatAgentService CreateService(
        IBus bus,
        WorkOrderReformatAgent agent,
        ILogger<AutoReformatAgentService>? logger = null,
        AutoReformatOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(bus);
        services.AddSingleton(agent);
        var provider = services.BuildServiceProvider();
        return new AutoReformatAgentService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            logger ?? NullLogger<AutoReformatAgentService>.Instance,
            Options.Create(options ?? new AutoReformatOptions()),
            TimeProvider.System);
    }

    private static IConfiguration ConfigurationWithDisable(string value) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [AutoReformatOptions.DisableConfigurationKey] = value
            })
            .Build();

    private static WorkOrder DraftWorkOrder(string number, string title = "t", string description = "d") =>
        new()
        {
            Number = number,
            Status = WorkOrderStatus.Draft,
            Title = title,
            Description = description
        };

    private sealed class StubReformatAgent : WorkOrderReformatAgent
    {
        private readonly WorkOrderReformatAgent.ReformatResult? _result;
        private readonly Exception? _exception;

        public StubReformatAgent(
            WorkOrderReformatAgent.ReformatResult? result = null,
            Exception? exception = null)
            : base(new ChatClientFactory(new StubUnavailableBus()), NullLogger<WorkOrderReformatAgent>.Instance)
        {
            _result = result;
            _exception = exception;
        }

        public override Task<WorkOrderReformatAgent.ReformatResult?> ReformatWorkOrderAsync(WorkOrder workOrder)
        {
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_result);
        }
    }

    private sealed class StubThrowingBus : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request) =>
            throw new InvalidOperationException("reformat-query-failed");

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => throw new NotSupportedException();
    }

    private sealed class StubDraftWorkOrderBus(WorkOrder[] workOrders) : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderSpecificationQuery)
            {
                return Task.FromResult((TResponse)(object)workOrders);
            }

            throw new NotSupportedException();
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => throw new NotSupportedException();
    }

    private sealed class StubRecordingBus(WorkOrder[] workOrders) : IBus
    {
        public object? LastCommand { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderSpecificationQuery)
            {
                return Task.FromResult((TResponse)(object)workOrders);
            }

            if (request is ReformatWorkOrderCommand command)
            {
                LastCommand = command;
                return Task.FromResult((TResponse)(object)workOrders[0]);
            }

            throw new NotSupportedException();
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => throw new NotSupportedException();
    }

    private sealed class StubUnavailableBus : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request) => throw new NotSupportedException();

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => throw new NotSupportedException();
    }
}
