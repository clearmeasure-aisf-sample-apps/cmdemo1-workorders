using System.Globalization;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

/// <summary>
/// Formats work-order dates with the client culture. On the WebAssembly host the layout
/// applies the browser language once the host is running.
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

        return Match(language.Trim());
    }

    private static CultureInfo? Match(string tag)
    {
        var direct = TryGet(tag);
        if (direct is not null)
        {
            return direct;
        }

        return MatchParts(tag.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static CultureInfo? MatchParts(string[] parts)
    {
        if (parts.Length >= 2)
        {
            var region = TryGet($"{parts[0]}-{parts[1]}");
            if (region is not null)
            {
                return region;
            }
        }

        if (parts.Length >= 1)
        {
            return TryGet(parts[0]);
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
