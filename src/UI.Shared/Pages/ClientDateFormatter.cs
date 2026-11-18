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

        var tag = Sanitize(language);
        return tag is null ? null : TryGet(tag);
    }

    private static string? Sanitize(string language)
    {
        var tag = language.Trim().Split('@', '.')[0].Replace('_', '-');
        return IsPosixDefault(tag) ? null : tag;
    }

    private static bool IsPosixDefault(string tag)
        => tag.Length == 0 || tag.Equals("C", StringComparison.OrdinalIgnoreCase) || tag.Equals("POSIX", StringComparison.OrdinalIgnoreCase);

    private static CultureInfo? TryGet(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
