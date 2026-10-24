using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

/// <summary>
/// Formats work-order dates with the client culture. On the WebAssembly host that culture
/// is the browser language applied at startup.
/// </summary>
internal static class ClientDateFormatter
{
    internal static string? Format(DateOnly? value) =>
        value?.ToString("d", CultureInfo.CurrentCulture);

    internal static string Format(DateTime value) =>
        value.ToString("G", CultureInfo.CurrentCulture);

    internal static string? Format(DateTime? value) =>
        value is { } date ? Format(date) : null;

    /// <summary>
    /// Points the current cultures at the browser language tag. A blank or unknown tag leaves them unchanged.
    /// </summary>
    internal static void ApplyBrowserCulture(string? language)
    {
        var culture = Resolve(language);
        if (culture is null)
        {
            return;
        }

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    private static CultureInfo? Resolve(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var tag = language.Trim();
        if (TryGet(tag) is { } culture)
        {
            return culture;
        }

        var parts = tag.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2 && TryGet($"{parts[0]}-{parts[1]}") is { } region)
        {
            return region;
        }

        if (parts.Length >= 1 && TryGet(parts[0]) is { } languageOnly)
        {
            return languageOnly;
        }

        return null;
    }

    private static CultureInfo? TryGet(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (Exception ex) when (ex is CultureNotFoundException or ArgumentException)
        {
            return null;
        }
    }
}
