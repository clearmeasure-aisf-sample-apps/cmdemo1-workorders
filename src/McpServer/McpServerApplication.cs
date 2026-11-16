using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ChurchBulletin.ServiceDefaults;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// Host bootstrap for standalone MCP server startup.
/// </summary>
public static class McpServerApplication
{
    /// <summary>
    /// Determines whether the MCP server should use HTTP transport.
    /// </summary>
    public static bool ShouldUseHttpTransport(string[] args, IConfiguration configuration) =>
        args.Contains("--http") ||
        string.Equals(configuration["Transport"], "http", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Builds and runs the MCP server host.
    /// </summary>
    public static async Task RunAsync(string[] args)
    {
        var app = BuildApplication(args);
        await app.RunAsync();
    }

    internal static WebApplication BuildApplication(string[] args, Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(args);

        AddSerilog(builder);

        builder.Services.AddMcpServerServices();

        var mcpBuilder = AddMcpServerWithTools(builder.Services);

        configureBuilder?.Invoke(builder);

        var useHttp = ShouldUseHttpTransport(args, builder.Configuration);
        ConfigureTransport(mcpBuilder, useHttp);

        var app = builder.Build();

        app.UseSerilogShutdown();
        ConfigurePipeline(app, useHttp);

        return app;
    }

    /// <summary>
    /// Compact JSON on stdout, as <see cref="SerilogExtensions.AddSerilogJsonConsole"/> writes for the other hosts, with
    /// the levels from this host's <c>appsettings.json</c> set in code: reading Serilog settings from configuration
    /// relies on reflection that Native AOT does not support.
    /// </summary>
    private static void AddSerilog(WebApplicationBuilder builder) =>
        builder.Host.UseSerilog(
            (context, configuration) => configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
                .WriteTo.Console(new RenderedCompactJsonFormatter()),
            writeToProviders: SerilogExtensions.ShouldWriteToProviders(builder.Environment));

    private static IMcpServerBuilder AddMcpServerWithTools(IServiceCollection services) =>
        services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new()
                {
                    Name = "ChurchBulletin",
                    Version = "1.0.0"
                };
            })
            .WithRequestFilters(filters => filters.AddCallToolFilter(FeatureDisabledToolFilter.Create))
            .WithTools<WorkOrderTools>(McpJsonContext.ToolOptions)
            .WithTools<EmployeeTools>(McpJsonContext.ToolOptions)
            .WithTools<TeamTools>(McpJsonContext.ToolOptions)
            .WithTools<VendorTools>(McpJsonContext.ToolOptions)
            .WithResources<ReferenceResources>();

    private static void ConfigureTransport(IMcpServerBuilder mcpBuilder, bool useHttp)
    {
        if (useHttp)
        {
            mcpBuilder.WithHttpTransport();
        }
        else
        {
            mcpBuilder.WithStdioServerTransport();
        }
    }

    private static void ConfigurePipeline(WebApplication app, bool useHttp)
    {
        if (useHttp)
        {
            app.UseCorrelationId();
            app.MapMcp("/mcp");
        }
    }
}
