using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Tools;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

/// <summary>
/// Builds the standalone MCP server services from <see cref="McpServiceRegistry"/> to prove its own MediatR
/// pipeline enforces <see cref="RequiresFeatureAttribute"/> and answers <see cref="FeatureFlagsQuery"/>.
/// </summary>
[TestFixture]
public class McpFeatureFlagBehaviorTests
{
    [Test]
    public async Task Send_Should_ThrowFeatureDisabled_When_TaggedRequestFlagIsOff()
    {
        await using var provider = BuildProvider(vendorManagement: false);
        var bus = provider.GetRequiredService<IBus>();

        var ex = await Should.ThrowAsync<FeatureDisabledException>(() => bus.Send(new McpGatedProbeRequest()));

        ex.FeatureName.ShouldBe(FeatureFlagNames.VendorManagement);
    }

    [Test]
    public async Task Send_Should_RunHandler_When_TaggedRequestFlagIsOn()
    {
        await using var provider = BuildProvider(vendorManagement: true);
        var bus = provider.GetRequiredService<IBus>();

        (await bus.Send(new McpGatedProbeRequest())).ShouldBe("handled");
    }

    [Test]
    public async Task Send_Should_ReturnBothManagementFlags_When_FeatureFlagsQuerySent()
    {
        await using var provider = BuildProvider(vendorManagement: true);
        var bus = provider.GetRequiredService<IBus>();

        var result = await bus.Send(new FeatureFlagsQuery());

        result.Flags[FeatureFlagNames.VendorManagement].ShouldBeTrue();
        result.Flags[FeatureFlagNames.BuildingManagement].ShouldBeFalse();
    }

    [Test]
    public async Task VendorTools_Should_ThrowFeatureDisabled_When_VendorManagementIsOff()
    {
        await using var provider = BuildProvider(vendorManagement: false);
        var bus = provider.GetRequiredService<IBus>();

        var list = await Should.ThrowAsync<FeatureDisabledException>(() => VendorTools.ListVendors(bus));
        list.FeatureName.ShouldBe(FeatureFlagNames.VendorManagement);
        list.RequestName.ShouldBe(nameof(VendorsQuery));

        var create = await Should.ThrowAsync<FeatureDisabledException>(() =>
            VendorTools.CreateVendor(bus, "Acme", "HVAC"));
        create.FeatureName.ShouldBe(FeatureFlagNames.VendorManagement);
        create.RequestName.ShouldBe(nameof(CreateVendorCommand));
    }

    private static ServiceProvider BuildProvider(bool vendorManagement)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureFlags:VendorManagement"] = vendorManagement ? "true" : "false"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddMcpServerServices();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddTransient<IRequestHandler<McpGatedProbeRequest, string>, McpGatedProbeHandler>();
        return services.BuildServiceProvider();
    }

    [RequiresFeature(FeatureFlagNames.VendorManagement)]
    public record McpGatedProbeRequest : IRequest<string>;

    public sealed class McpGatedProbeHandler : IRequestHandler<McpGatedProbeRequest, string>
    {
        public Task<string> Handle(McpGatedProbeRequest request, CancellationToken cancellationToken) =>
            Task.FromResult("handled");
    }
}
