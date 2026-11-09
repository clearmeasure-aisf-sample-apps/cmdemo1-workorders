using Asp.Versioning;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Messaging;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Server.BuildFacts;
using ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using ClearMeasure.Bootcamp.UI.Server.Testing;
using ChurchBulletin.ServiceDefaults;
using Microsoft.AspNetCore.ResponseCompression;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// The services UI.Server registers: the web framework (MVC, API versioning, Razor, Lamar), the request protections
/// in front of the API, response caching and compression, and the embedded MCP server.
/// </summary>
public static class ServerServices
{
    public static void AddServerServices(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.Configuration.AddEnvironmentVariables();
        AddWebFramework(builder);
        AddRequestProtections(builder.Services, builder.Configuration);
        AddResponseCaching(builder.Services);
        AddEmbeddedMcpServer(builder.Services);
    }

    private static void AddWebFramework(WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails();
        builder.Services.AddControllersWithViews()
            .AddApplicationPart(typeof(DetailedHealthController).Assembly);
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
            options.UnsupportedApiVersionStatusCode = StatusCodes.Status400BadRequest;
        }).AddMvc();
        builder.Services.AddRazorPages();
        builder.Host.UseLamar(registry => { registry.IncludeRegistry<UiServiceRegistry>(); });
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddLiveTelemetry();
        builder.Services.AddBuildFacts();
        builder.Services.AddScoped<IDistributedBus, DistributedBus>();
        builder.Services.AddMemoryCache();
        builder.Services.AddGrpc();
        builder.Services.Configure<LlmHealthCheckOptions>(builder.Configuration.GetSection("LlmHealthCheck"));
    }

    /// <summary>Idempotency, rate limits, timeouts, API keys, body buffering, decompression and CORS.</summary>
    private static void AddRequestProtections(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IdempotencyOptions>(configuration.GetSection(IdempotencyOptions.SectionName));
        services.AddSingleton<IdempotencyProbeState>();
        services.AddApiRateLimiting(configuration);
        services.AddApiRequestTimeouts(configuration);
        services.Configure<ApiKeyAuthenticationOptions>(configuration.GetSection(ApiKeyAuthenticationOptions.SectionName));
        services.PostConfigure<ApiKeyAuthenticationOptions>(o =>
            o.ValidationKey = string.IsNullOrWhiteSpace(o.ValidationKey) ? null : o.ValidationKey.Trim());
        services.AddRequestDecompression();
        services.Configure<RequestBodyBufferingOptions>(configuration.GetSection(RequestBodyBufferingOptions.SectionName));
        services.AddServerCors(configuration);
    }

    private static void AddResponseCaching(IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            options.AddBasePolicy(policy => policy.NoCache());
            options.AddPolicy(OutputCachePolicyNames.VersionMetadata, policy => policy
                .Expire(TimeSpan.FromMinutes(10))
                .SetVaryByQuery("*")
                .SetVaryByHeader("Accept"));
            options.AddPolicy(OutputCachePolicyNames.WeatherSample, policy => policy
                .Expire(TimeSpan.FromSeconds(30))
                .SetVaryByQuery("*")
                .SetVaryByHeader("Accept"));
        });
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
    }

    private static void AddEmbeddedMcpServer(IServiceCollection services) =>
        services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new() { Name = "ChurchBulletin", Version = "1.0.0" };
            })
            .WithHttpTransport()
            .WithRequestFilters(filters => filters.AddCallToolFilter(FeatureDisabledToolFilter.Create))
            .WithTools<WorkOrderTools>()
            .WithTools<EmployeeTools>()
            .WithTools<TeamTools>()
            .WithTools<VendorTools>()
            .WithResources<ReferenceResources>();
}
