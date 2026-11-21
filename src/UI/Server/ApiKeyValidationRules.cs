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
               && ApiKeyAuthenticationMiddleware.IsApiPath(pathValue)
               && !ApiKeyAuthenticationMiddleware.IsPublicVersionOrTimePath(pathValue);
    }
}
