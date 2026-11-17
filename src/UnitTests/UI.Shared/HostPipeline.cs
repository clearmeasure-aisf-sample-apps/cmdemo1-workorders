using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.McpServer;
using Microsoft.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

/// <summary>
/// The request pipeline exactly as the MCP host composes it (source-generated mediator, validation then feature-flag
/// behaviors, the in-process validators), with a test feature-flag evaluator and test handlers registered last so they
/// replace the real ones. The generated mediator resolves each handler as <c>IRequestHandler&lt;TRequest, TResponse&gt;</c>.
/// </summary>
internal static class HostPipeline
{
    public static ServiceProvider Build(IFeatureFlagEvaluator evaluator, Action<IServiceCollection> registerHandlers)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMcpServerServices();
        services.AddSingleton(evaluator);
        registerHandlers(services);
        return services.BuildServiceProvider();
    }
}