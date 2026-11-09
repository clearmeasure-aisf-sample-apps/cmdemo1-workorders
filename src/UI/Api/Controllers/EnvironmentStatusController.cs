using System.Reflection;
using System.Runtime.InteropServices;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Runtime environment snapshot for operations and support. Environment variable
/// values are never returned.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/status/environment")]
[Route($"{ApiRoutes.VersionedApiPrefix}/status/environment")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class EnvironmentStatusController : ControllerBase
{
    public const string RedactedValue = EchoController.RedactedValue;
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

    /// <summary>
    /// Returns OS description, processor count, CLR version, and selected env var names.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var payload = BuildResponse();
        var etag = ConditionalGetEtag.CreateWeakEtagForJson(payload);
        Response.Headers.ETag = etag.ToString();
        if (ConditionalGetEtag.IfNoneMatchIncludesEtag(Request, etag))
            return StatusCode(StatusCodes.Status304NotModified);
        return ConditionalGetEtag.JsonContent(payload);
    }

    private static EnvironmentStatusResponse BuildResponse()
    {
        var names = ReportedEnvironmentVariableNames
            .Where(name => Environment.GetEnvironmentVariable(name) is not null)
            .ToList();
        return new EnvironmentStatusResponse(
            OsDescription: RuntimeInformation.OSDescription,
            ProcessorCount: Environment.ProcessorCount,
            ClrVersion: Environment.Version.ToString(),
            EnvironmentVariableNames: names,
            EnvironmentVariables: names.ToDictionary(name => name, _ => RedactedValue, StringComparer.OrdinalIgnoreCase),
            Version: EntryAssemblyVersion(),
            GitSha: EntryAssemblySourceRevision(),
            EnvironmentName: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "unknown");
    }

    private static string EntryAssemblyVersion() =>
        Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";

    private static string EntryAssemblySourceRevision() =>
        Assembly.GetEntryAssembly()
            ?.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "SourceRevisionId")
            ?.Value ?? "unknown";
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
