using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Shared;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Runs the UI.Server host with a request tagged <see cref="RequiresFeatureAttribute"/> to prove the
/// <see cref="FeatureFlagBehavior{TRequest,TResponse}"/> registration, the runtime evaluator, and the single-API
/// 403 mapping work together. The tagged request is the real <see cref="VendorsQuery"/> with a recording handler,
/// because the source-generated mediator only routes message types it saw at build time.
/// </summary>
[TestFixture]
public class FeatureFlagBehaviorIntegrationTests
{
    [Test]
    public async Task Send_Should_ThrowFeatureDisabled_When_TaggedRequestFlagIsOff()
    {
        await using var factory = new GatedProbeWebApplicationFactory(vendorManagement: false);
        using var scope = factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        var ex = await Should.ThrowAsync<FeatureDisabledException>(() => bus.Send(new VendorsQuery()));

        ex.FeatureName.ShouldBe(FeatureFlagNames.VendorManagement);
        factory.Services.GetRequiredService<RecordingVendorsHandler>().Calls.ShouldBe(0);
    }

    [Test]
    public async Task Send_Should_RunHandler_When_TaggedRequestFlagIsOn()
    {
        await using var factory = new GatedProbeWebApplicationFactory(vendorManagement: true);
        using var scope = factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        var result = await bus.Send(new VendorsQuery());

        result.Select(vendor => vendor.Name).ShouldBe([RecordingVendorsHandler.VendorName]);
        factory.Services.GetRequiredService<RecordingVendorsHandler>().Calls.ShouldBe(1);
    }

    [Test]
    public async Task SingleApi_Should_Return403Problem_When_TaggedRequestFlagIsOff()
    {
        await using var factory = new GatedProbeWebApplicationFactory(vendorManagement: false);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/blazor-wasm-single-api", new WebServiceMessage(new VendorsQuery()));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("status").GetInt32().ShouldBe(403);
        problem.RootElement.GetProperty("title").GetString().ShouldBe("Feature disabled");
        problem.RootElement.GetProperty("feature").GetString().ShouldBe(FeatureFlagNames.VendorManagement);
        problem.RootElement.GetProperty("detail").GetString()!.ShouldContain(nameof(VendorsQuery));
    }

    [Test]
    public async Task SingleApi_Should_ReturnHandlerResult_When_TaggedRequestFlagIsOn()
    {
        await using var factory = new GatedProbeWebApplicationFactory(vendorManagement: true);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/blazor-wasm-single-api", new WebServiceMessage(new VendorsQuery()));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var message = JsonSerializer.Deserialize<WebServiceMessage>(await response.Content.ReadAsStringAsync())!;
        message.GetBodyObject().ShouldBeOfType<Vendor[]>().Select(vendor => vendor.Name)
            .ShouldBe([RecordingVendorsHandler.VendorName]);
    }

    [Test]
    public async Task SingleApi_Should_DenyBuildingQueries_When_BuildingManagementIsOff()
    {
        await using var factory = new GatedProbeWebApplicationFactory(vendorManagement: false);
        using var client = factory.CreateClient();

        foreach (var query in new IRemotableRequest[] { new BuildingRoomSummaryQuery(), new WorkOrderBuildingsQuery() })
        {
            using var response = await client.PostAsJsonAsync(
                "/api/blazor-wasm-single-api", new WebServiceMessage(query));

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, query.GetType().Name);
            response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            problem.RootElement.GetProperty("status").GetInt32().ShouldBe(403);
            problem.RootElement.GetProperty("feature").GetString().ShouldBe(FeatureFlagNames.BuildingManagement);
            problem.RootElement.GetProperty("detail").GetString()!.ShouldContain(query.GetType().Name);
        }
    }

    [Test]
    public async Task Send_Should_DenyBuildingQueries_When_BuildingManagementIsOff()
    {
        await using var factory = new GatedProbeWebApplicationFactory(vendorManagement: false);
        using var scope = factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        var summary = await Should.ThrowAsync<FeatureDisabledException>(() => bus.Send(new BuildingRoomSummaryQuery()));
        summary.FeatureName.ShouldBe(FeatureFlagNames.BuildingManagement);
        summary.RequestName.ShouldBe(nameof(BuildingRoomSummaryQuery));

        var buildings = await Should.ThrowAsync<FeatureDisabledException>(() => bus.Send(new WorkOrderBuildingsQuery()));
        buildings.FeatureName.ShouldBe(FeatureFlagNames.BuildingManagement);
        buildings.RequestName.ShouldBe(nameof(WorkOrderBuildingsQuery));
    }

    [Test]
    public async Task SingleApi_Should_ReturnBothManagementFlags_When_FeatureFlagsQueryPosted()
    {
        await using var factory = new GatedProbeWebApplicationFactory(vendorManagement: true);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/blazor-wasm-single-api", new WebServiceMessage(new FeatureFlagsQuery()));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var message = JsonSerializer.Deserialize<WebServiceMessage>(await response.Content.ReadAsStringAsync())!;
        var result = message.GetBodyObject().ShouldBeOfType<FeatureFlagsResult>();
        result.Flags.Keys.ShouldBe(FeatureFlagNames.All, ignoreOrder: true);
        result.Flags[FeatureFlagNames.VendorManagement].ShouldBeTrue();
        result.Flags[FeatureFlagNames.BuildingManagement].ShouldBeFalse();
        result.Flags[FeatureFlagNames.ShowPriorityBadges].ShouldBeTrue();
    }

    [Test]
    public async Task Send_Should_DenyVendorDirectory_When_VendorManagementIsOff()
    {
        await using var factory = new GatedProbeWebApplicationFactory(vendorManagement: false);
        using var scope = factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        var list = await Should.ThrowAsync<FeatureDisabledException>(() => bus.Send(new VendorsQuery()));
        list.FeatureName.ShouldBe(FeatureFlagNames.VendorManagement);
        list.RequestName.ShouldBe(nameof(VendorsQuery));

        var create = await Should.ThrowAsync<FeatureDisabledException>(() =>
            bus.Send(new CreateVendorCommand("Acme", "HVAC", null, null, null)));
        create.FeatureName.ShouldBe(FeatureFlagNames.VendorManagement);
        create.RequestName.ShouldBe(nameof(CreateVendorCommand));
    }

    /// <summary>Stands in for the database-backed vendor handler; one instance per host so calls can be counted.</summary>
    public sealed class RecordingVendorsHandler : IRequestHandler<VendorsQuery, Vendor[]>
    {
        public const string VendorName = "Probe Vendor";

        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public ValueTask<Vendor[]> Handle(VendorsQuery request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return ValueTask.FromResult(new[] { new Vendor { Name = VendorName } });
        }
    }

    private sealed class GatedProbeWebApplicationFactory(bool vendorManagement)
        : WebApplicationFactory<UiServerWebApplicationMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:SqlConnectionString", "Data Source=:memory:");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:SqlConnectionString"] = "Data Source=:memory:",
                    ["AI_OpenAI_ApiKey"] = "",
                    ["AI_OpenAI_Url"] = "",
                    ["AI_OpenAI_Model"] = "",
                    ["ApiKeyAuthentication:Enabled"] = "false",
                    ["ApiKeyAuthentication:ValidationKey"] = "",
                    ["FeatureFlags:VendorManagement"] = vendorManagement ? "true" : "false"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<RecordingVendorsHandler>();
                services.AddTransient<IRequestHandler<VendorsQuery, Vendor[]>>(
                    provider => provider.GetRequiredService<RecordingVendorsHandler>());
                services.AddSingleton<IValidator<VendorsQuery>, InlineValidator<VendorsQuery>>();
            });
        }
    }
}
