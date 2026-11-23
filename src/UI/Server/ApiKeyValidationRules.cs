namespace ClearMeasure.Bootcamp.UI.Server;

internal static class ApiKeyValidationRules
{
    internal static bool RequiresValidation(string? pathValue, ApiKeyAuthenticationOptions options)
    {
        if (!options.IsConfigured)
        {
            return false;
        }

        return ApiRoutePolicy.IsApiPath(pathValue) && !ApiPublicPathRules.IsPublicApiPath(pathValue);
    }
}
