using System.Reflection;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Which build and environment the process reports; "unknown" when they cannot be determined.
/// </summary>
public sealed record DeploymentIdentity(string GitSha, string EnvironmentName)
{
    private const string Unknown = "unknown";

    public static DeploymentIdentity Unreported { get; } = new(Unknown, Unknown);

    /// <summary>The short commit shown in the footer, or <c>null</c> when the commit is not known.</summary>
    public string? ShortSha => GitSha != Unknown && GitSha.Length >= 7 ? GitSha[..7] : null;

    /// <summary>
    /// Reads git SHA and environment name the same way <c>GET /api/status/environment</c> does.
    /// </summary>
    public static DeploymentIdentity FromProcess() =>
        new(EntryAssemblySourceRevision(), ProcessEnvironmentName());

    private static string ProcessEnvironmentName() =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
        ?? Unknown;

    private static string EntryAssemblySourceRevision() =>
        Assembly.GetEntryAssembly()
            ?.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "SourceRevisionId")
            ?.Value ?? Unknown;
}
