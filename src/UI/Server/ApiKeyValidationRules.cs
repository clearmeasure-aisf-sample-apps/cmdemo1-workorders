namespace ClearMeasure.Bootcamp.UI.Server;

internal static class ApiKeyValidationRules
{
    internal static bool RequiresValidation(string? pathValue, ApiKeyAuthenticationOptions options)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ValidationKey))
        {
            return false;
        }

        return !string.IsNullOrEmpty(pathValue)
               && IsApiPath(pathValue)
               && !ApiPublicPathRules.IsPublicApiPath(pathValue);
    }

    private static bool IsApiPath(string pathValue) =>
        pathValue.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
        || pathValue.Equals("/api", StringComparison.OrdinalIgnoreCase);
}
