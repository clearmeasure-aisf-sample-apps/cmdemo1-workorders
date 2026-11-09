using System.Reflection;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server.BuildFacts;
using ClearMeasure.Bootcamp.UI.Server.Grpc;
using ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using ClearMeasure.Bootcamp.UI.Server.Notifications;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using ClearMeasure.Bootcamp.UI.Server.Testing;
using ChurchBulletin.ServiceDefaults;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// The UI.Server request pipeline, in order: observability, error handling, transport and static files, the guards
/// in front of the API, request processing, then the endpoints. Middleware order matters; keep each step's position.
/// </summary>
public static class ServerPipeline
{
    public static void UseServerPipeline(this WebApplication app)
    {
        app.UseSerilogShutdown();
        app.MapDefaultEndpoints();
        UseObservability(app);
        UseErrorHandling(app);
        UseTransportAndStaticFiles(app);
        UseApiGuards(app);
        UseRequestProcessing(app);
        MapApplicationEndpoints(app);
        MapOperationalEndpoints(app);
    }

    private static void UseObservability(WebApplication app)
    {
        app.UseMiddleware<HttpRequestMetricsMiddleware>();
        app.UseMiddleware<LiveTelemetryMiddleware>();
        app.UseCorrelationId();
    }

    private static void UseErrorHandling(WebApplication app)
    {
        app.UseWhen(
            context => ProblemDetailsPaths.IsMachineOriented(context.Request.Path),
            branch => branch.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = context => ProblemDetailsExceptionHandler.HandleAsync(context, app.Environment)
            }));

        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
    }

    private static void UseTransportAndStaticFiles(WebApplication app)
    {
        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseHttpsRedirection();
        }

        app.UseRequestDecompression();
        app.UseResponseCompression();
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.UseRouting();
    }

    private static void UseApiGuards(WebApplication app)
    {
        app.UseWhen(
            context => ApiRateLimitingExtensions.ShouldApplyToPath(context.Request.Path),
            branch => branch.UseRequestTimeouts());
        app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        app.UseMachineClientStatusCodeProblemDetails();
        if (app.Services.IsServerCorsActive())
        {
            app.UseCors(ServerCorsOptions.PolicyName);
        }
    }

    private static void UseRequestProcessing(WebApplication app)
    {
        app.UseWebSockets();
        app.UseMiddleware<RealtimeNotificationWebSocketMiddleware>();
        app.MapTestingEndpoints();
        app.UseRequestBodyBuffering();
        app.UseMiddleware<IdempotencyMiddleware>();
        app.UseMiddleware<WebServiceMessageValidationMiddleware>();
        app.UseMiddleware<RateLimitingMiddleware>();
        app.UseOutputCache();
    }

    private static void MapApplicationEndpoints(WebApplication app)
    {
        app.MapRazorPages();
        var apiControllers = app.MapControllers();
        ApiControllerMapping.ApplyRequestTimeout(app, apiControllers);
        ApiControllerMapping.ApplyCorsWhenActive(app, apiControllers);
        app.MapGrpcService<WorkOrdersGrpcService>();
        app.MapMcp("/mcp");
        app.MapFallback(FallbackToIndexHtml);
    }

    /// <summary>Reboot demo switch, version, live telemetry, build facts and health checks.</summary>
    private static void MapOperationalEndpoints(WebApplication app)
    {
        app.MapGet("/_demo/setneedsreboot/{value:bool}", (bool value) =>
        {
            NeedsRebootHealthCheck.NeedsReboot = value;
            return Results.Text($"NeedsReboot set to {value}");
        });
        app.MapGet("/_version", () => Results.Json(new { version = InformationalVersion() }))
            .CacheOutput(OutputCachePolicyNames.VersionMetadata);
        app.MapLiveTelemetry();
        app.MapBuildFacts();
        app.MapHealthChecks("_healthcheck", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckPlainTextResponseWriter.WriteAsync
        });
        app.MapHealthChecks("_healthcheck/detailed", new HealthCheckOptions
        {
            ResponseWriter = DetailedHealthCheckResponseWriter.WriteAsync
        });
    }

    private static string? InformationalVersion() =>
        typeof(ServerPipeline).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    private static async Task FallbackToIndexHtml(HttpContext context)
    {
        if (ProblemDetailsPaths.IsMachineOriented(context.Request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var fileInfo = env.WebRootFileProvider.GetFileInfo("index.html");
        if (!fileInfo.Exists)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await using var stream = fileInfo.CreateReadStream();
        await stream.CopyToAsync(context.Response.Body);
    }
}
