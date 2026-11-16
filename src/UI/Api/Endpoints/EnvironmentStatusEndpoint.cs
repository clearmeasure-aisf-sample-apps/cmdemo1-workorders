using System.Reflection;
using System.Runtime.InteropServices;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// <c>GET api/status/environment</c>: runtime environment snapshot for operations and support. Environment variable
/// values are never returned.
/// </summary>
public static class EnvironmentStatusEndpoint
{
    public const string RedactedValue = EchoEndpoint.RedactedValue;
    public const string RedactionProbeVariableName = "TEST_ENV_STATUS_SECRET";

    private static readonly string[] ReportedEnvironmentVariableNames =
    [
        "ASPNETCORE_ENVIRONMENT",
        "APPLICATIONINSIGHTS_CONNECTION_STRING",
        "DOTNET_ENVIRONMENT",
        "DOTNET_ROOT",
        "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT",
        RedactionProbeVariableName
    ];

    public static void Map(IEndpointRouteBuilder api) => api.MapGet("status/environment", Get);

    /// <summary>
    /// Returns OS description, processor count, CLR version, and selected env var names.
    /// </summary>
    public static IResult Get(HttpContext context) => ConditionalGetEtag.ConditionalJson(context, BuildResponse());

    private static EnvironmentStatusResponse BuildResponse()
    {
        var names = ReportedEnvironmentVariableNames
            .Where(name => Environment.GetEnvironmentVariable(name) is not null)
            .ToList();
        var deployment = DeploymentIdentity.FromProcess();
        return new EnvironmentStatusResponse(
            OsDescription: RuntimeInformation.OSDescription,
            ProcessorCount: Environment.ProcessorCount,
            ClrVersion: Environment.Version.ToString(),
            EnvironmentVariableNames: names,
            EnvironmentVariables: names.ToDictionary(name => name, _ => RedactedValue, StringComparer.OrdinalIgnoreCase),
            Version: EntryAssemblyVersion(),
            GitSha: deployment.GitSha,
            EnvironmentName: deployment.EnvironmentName);
    }

    private static string EntryAssemblyVersion() =>
        Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";
}

/// <summary>
/// JSON payload for <c>GET /api/status/environment</c> and the versioned twin.
/// </summary>
public record EnvironmentStatusResponse(
    string OsDescription,
    int ProcessorCount,
    string ClrVersion,
    IReadOnlyList<string> EnvironmentVariableNames,
    IReadOnlyDictionary<string, string> EnvironmentVariables,
    string Version,
    string GitSha,
    string EnvironmentName);
