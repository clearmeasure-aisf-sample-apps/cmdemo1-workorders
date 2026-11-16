using System.Reflection;
using System.Runtime.InteropServices;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// Exposes build and deployment metadata for operators and integrations.
/// </summary>
public static class VersionEndpoint
{
    public static void Map(IEndpointRouteBuilder api) =>
        api.MapGet("version", Get).AllowAnonymous().CacheOutput(OutputCachePolicyNames.VersionMetadata);

    /// <summary>
    /// Returns assembly, runtime, and host metadata.
    /// </summary>
    public static IResult Get(HttpContext context, IHostEnvironment hostEnvironment)
    {
        var assembly = typeof(VersionEndpoint).Assembly;
        var payload = new VersionMetadataResponse(
            AssemblyVersion: assembly.GetName().Version?.ToString(),
            InformationalVersion: assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            BuildConfiguration: ResolveBuildConfiguration(assembly),
            Environment: hostEnvironment.EnvironmentName,
            MachineName: Environment.MachineName,
            FrameworkDescription: RuntimeInformation.FrameworkDescription,
            OsDescription: RuntimeInformation.OSDescription);
        return ConditionalGetEtag.ConditionalJson(context, payload);
    }

    private static string ResolveBuildConfiguration(Assembly assembly)
    {
        var fromAttribute = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;
        if (!string.IsNullOrWhiteSpace(fromAttribute))
            return fromAttribute;

#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }
}

/// <summary>
/// JSON payload for <c>GET /api/version</c> and <c>GET /api/v1.0/version</c>.
/// </summary>
public record VersionMetadataResponse(
    string? AssemblyVersion,
    string? InformationalVersion,
    string BuildConfiguration,
    string? Environment,
    string MachineName,
    string FrameworkDescription,
    string OsDescription);

