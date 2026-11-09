using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Shared;
using FluentValidation;
using MediatR;
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
/// 403 mapping work together.
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

        string value = $"off-{Guid.NewGuid():N}";

        var ex = await Should.ThrowAsync<FeatureDisabledException>(() => bus.Send(new GatedProbeRequest(value)));

        ex.FeatureName.ShouldBe(FeatureFlagNames.VendorManagement);
        lock (GatedProbeHandler.Calls)
        {
            GatedProbeHandler.Calls.ShouldNotContain(value);
        }
    }

    [Test]
    public async Task Send_Should_RunHandler_When_TaggedRequestFlagIsOn()
    {
        await using var factory = new GatedProbeWebApplicationFactory(vendorManagement: true);
        using var scope = factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        string result = await bus.Send(new GatedProbeRequest("ping"));

        result.ShouldBe("pong:ping");
    }

    [Test]
    public async Task SingleApi_Should_Return403Problem_When_TaggedRequestFlagIsOff()
    {
        await using var factory = new GatedProbeWebApplicationFactory(vendorManagement: false);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/blazor-wasm-single-api", new WebServiceMessage(new GatedProbeRequest("ping")));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("status").GetInt32().ShouldBe(403);
        problem.RootElement.GetProperty("title").GetString().ShouldBe("Feature disabled");
        problem.RootElement.GetProperty("feature").GetString().ShouldBe(FeatureFlagNames.VendorManagement);
        problem.RootElement.GetProperty("detail").GetString()!.ShouldContain(nameof(GatedProbeRequest));
    }

    [Test]
    public async Task SingleApi_Should_ReturnHandlerResult_When_TaggedRequestFlagIsOn()
    {
        await using var factory = new GatedProbeWebApplicationFactory(vendorManagement: true);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/blazor-wasm-single-api", new WebServiceMessage(new GatedProbeRequest("ping")));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var message = JsonSerializer.Deserialize<WebServiceMessage>(await response.Content.ReadAsStringAsync())!;
        message.GetBodyObject().ShouldBe("pong:ping");
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

    [RequiresFeature(FeatureFlagNames.VendorManagement)]
    public record GatedProbeRequest(string Value) : IRequest<string>, IRemotableRequest;

    public sealed class GatedProbeHandler : IRequestHandler<GatedProbeRequest, string>
    {
        public static List<string> Calls { get; } = [];

        public Task<string> Handle(GatedProbeRequest request, CancellationToken cancellationToken)
        {
            lock (Calls)
            {
                Calls.Add(request.Value);
            }

            return Task.FromResult($"pong:{request.Value}");
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
                services.AddTransient<IRequestHandler<GatedProbeRequest, string>, GatedProbeHandler>();
                services.AddSingleton<IValidator<GatedProbeRequest>, InlineValidator<GatedProbeRequest>>();
            });
        }
    }
}
