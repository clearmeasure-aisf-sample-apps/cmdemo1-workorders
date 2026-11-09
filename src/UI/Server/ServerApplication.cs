using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Host bootstrap for UI.Server: Kestrel, <see cref="ServerServices"/>, the <see cref="ServerMessagingEndpoint"/>
/// and the <see cref="ServerPipeline"/>.
/// </summary>
public static class ServerApplication
{
    /// <summary>
    /// Builds and runs the web application host.
    /// </summary>
    public static async Task RunAsync(string[] args)
    {
        var app = BuildApplication(args);
        await app.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();
        await app.RunAsync();
    }

    internal static WebApplication BuildApplication(
        string[] args,
        Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(options =>
            options.ConfigureEndpointDefaults(listenOptions => listenOptions.Protocols = HttpProtocols.Http1AndHttp2));
        builder.AddServerServices();
        configureBuilder?.Invoke(builder);
        ServerMessagingEndpoint.Register(builder);
        var app = builder.Build();
        app.UseServerPipeline();
        return app;
    }
}
