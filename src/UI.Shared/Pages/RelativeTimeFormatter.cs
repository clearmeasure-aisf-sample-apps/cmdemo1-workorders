namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

internal static class RelativeTimeFormatter
{
    internal static string? Format(DateTime? created, TimeProvider clock)
    {
        if (created is null)
        {
            return null;
        }

        var elapsed = clock.GetUtcNow().UtcDateTime - ToUtc(created.Value);
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        return Phrase(elapsed);
    }

    private static string Phrase(TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds < 60)
        {
            return "just now";
        }

        if (elapsed.TotalMinutes < 60)
        {
            return Ago((int)elapsed.TotalMinutes, "minute");
        }

        if (elapsed.TotalHours < 24)
        {
            return Ago((int)elapsed.TotalHours, "hour");
        }

        return DayOrMonth(elapsed);
    }

    private static string DayOrMonth(TimeSpan elapsed)
    {
        var days = (int)elapsed.TotalDays;
        if (days < 30)
        {
            return Ago(days, "day");
        }

        return Ago(Math.Max(1, days / 30), "month");
    }

    private static string Ago(int count, string unit)
    {
        var label = count == 1 ? unit : unit + "s";
        return $"{count} {label} ago";
    }

    private static DateTime ToUtc(DateTime created) =>
        created.Kind switch
        {
            DateTimeKind.Utc => created,
            DateTimeKind.Local => created.ToUniversalTime(),
            _ => DateTime.SpecifyKind(created, DateTimeKind.Utc)
        };
}
