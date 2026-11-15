using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
///     Background service that periodically evaluates draft work orders and reformats
///     their title and description fields using an AI agent, persisting through
///     <see cref="ReformatWorkOrderCommand"/> on the bus.
/// </summary>
public class AutoReformatAgentService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutoReformatAgentService> _logger;
    private readonly AutoReformatOptions _options;
    private readonly TimeProvider _timeProvider;

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public AutoReformatAgentService(
        IServiceScopeFactory scopeFactory,
        ILogger<AutoReformatAgentService> logger,
        IOptions<AutoReformatOptions> options,
        TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Disabled)
        {
            _logger.LogInformation(
                "AutoReformatAgentService disabled via {DisableKey} configuration",
                AutoReformatOptions.DisableConfigurationKey);
            return;
        }

        _logger.LogInformation("AutoReformatAgentService started");
        await RunLoopAsync(stoppingToken);
        _logger.LogInformation("AutoReformatAgentService stopped");
    }

    internal static bool IsDisabled(IConfiguration configuration) =>
        string.Equals(
            configuration[AutoReformatOptions.DisableConfigurationKey],
            "true",
            StringComparison.OrdinalIgnoreCase);

    private async Task RunLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && await TryRunOnceAsync(stoppingToken))
        {
        }
    }

    private async Task<bool> TryRunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ReformatWorkOrdersAsync(stoppingToken);
            await Task.Delay(_options.PollInterval, _timeProvider, stoppingToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AutoReformatAgentService execution");
            await Task.Delay(_options.ErrorRetryDelay, _timeProvider, stoppingToken);
            return true;
        }
    }

    internal async Task ReformatWorkOrdersAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();
        var agent = scope.ServiceProvider.GetRequiredService<WorkOrderReformatAgent>();

        try
        {
            var draftWorkOrders = await LoadDraftWorkOrdersAsync(bus);
            _logger.LogDebug("Found {Count} draft work orders to evaluate for reformatting", draftWorkOrders.Length);

            foreach (var workOrder in draftWorkOrders)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await TryReformatSingleWorkOrderAsync(bus, agent, workOrder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving draft work orders for reformatting");
        }
    }

    internal static async Task<WorkOrder[]> LoadDraftWorkOrdersAsync(IBus bus)
    {
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchStatus(WorkOrderStatus.Draft);
        return await bus.Send(specification);
    }

    private async Task TryReformatSingleWorkOrderAsync(
        IBus bus,
        WorkOrderReformatAgent agent,
        WorkOrder workOrder)
    {
        try
        {
            var result = await agent.ReformatWorkOrderAsync(workOrder);
            if (result is null)
            {
                return;
            }

            await PersistReformatAsync(bus, workOrder, result);
            _logger.LogInformation("Successfully reformatted WorkOrder {WorkOrderNumber}", workOrder.Number);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reformatting WorkOrder {WorkOrderNumber}", workOrder.Number);
        }
    }

    private static Task PersistReformatAsync(
        IBus bus,
        WorkOrder workOrder,
        WorkOrderReformatAgent.ReformatResult result) =>
        bus.Send(new ReformatWorkOrderCommand(workOrder.Number!, result.Title, result.Description));
}
