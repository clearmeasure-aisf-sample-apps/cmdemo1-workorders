namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Routes for the Blazor WASM abstract web-service endpoint (MediatR-over-HTTP).
/// </summary>
public static class WebServiceApiRoutes
{
    /// <summary>
    /// Path segment after <c>api/</c> (no leading slash).
    /// </summary>
    public const string AbstractPathSegment = "blazor-wasm-single-api";

    /// <summary>
    /// Legacy unversioned URL path (no leading slash).
    /// </summary>
    public const string LegacyRelativeUrl = "api/" + AbstractPathSegment;

    /// <summary>
    /// Versioned URL path for API version 1.0, the versioned twin of <see cref="LegacyRelativeUrl"/> (no leading slash).
    /// </summary>
    public const string VersionedRelativeUrl = "api/v1.0/" + AbstractPathSegment;
}
