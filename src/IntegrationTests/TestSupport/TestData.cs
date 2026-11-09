using System.Security.Cryptography;
using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.IntegrationTests.TestSupport;

/// <summary>
/// Unique, anonymized values for tests that share a database. A test that inserts rows
/// with these values and asserts only on them does not depend on, or disturb, rows other
/// tests created.
/// </summary>
public static class TestData
{
    private const string Base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int MaxNumberLength = 7;

    /// <summary>Eight hex characters, unique per call.</summary>
    public static string Tag()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }

    /// <summary>
    /// A unique work order number of seven characters: the optional one-character prefix
    /// followed by random uppercase base36 characters.
    /// </summary>
    public static string Number(string prefix = "")
    {
        if (prefix.Length > 1)
        {
            throw new ArgumentException("A work order number prefix is at most one character.", nameof(prefix));
        }

        return prefix + RandomBase36(MaxNumberLength - prefix.Length);
    }

    /// <summary>
    /// <paramref name="count"/> unique work order numbers that share a random five-character stem
    /// and end in 01, 02, ...; they sort ordinally in index order.
    /// </summary>
    public static string[] NumberSeries(int count)
    {
        if (count is < 1 or > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "A number series has 1 to 99 numbers.");
        }

        var stem = RandomBase36(5);
        return Enumerable.Range(1, count).Select(i => $"{stem}{i:00}").ToArray();
    }

    /// <summary>A unique user name: <paramref name="prefix"/>, an underscore and a <see cref="Tag"/>.</summary>
    public static string UserName(string prefix)
    {
        return $"{prefix}_{Tag()}";
    }

    /// <summary>An e-mail address on the reserved example.test domain for <paramref name="userName"/>.</summary>
    public static string Email(string userName)
    {
        return $"{userName}@example.test";
    }

    /// <summary>
    /// A clock fixed later than every date stored on a work order in the test database
    /// (at least 90 days, plus a random 0-29 days, at 12:00 UTC). Time windows that are open
    /// towards the future (completed in the last 30 days, opened in the last 7 days, completed
    /// per week over 8 weeks) then contain only rows the calling test dates relative to this clock.
    /// Overdue is open towards the past and is not isolated by this clock. Each call moves the
    /// stored dates 90-119 days further on, so a database that is reused for thousands of runs
    /// must eventually be recreated (the method throws past the year 9900).
    /// </summary>
    public static TimeProvider FutureClock()
    {
        using var db = TestHost.NewDbContext();
        var orders = db.Set<WorkOrder>().AsNoTracking();
        var latest = new[]
            {
                orders.Where(w => w.CreatedDate != null).Max(w => w.CreatedDate),
                orders.Where(w => w.AssignedDate != null).Max(w => w.AssignedDate),
                orders.Where(w => w.CompletedDate != null).Max(w => w.CompletedDate),
                DateTime.UtcNow
            }
            .Where(d => d.HasValue)
            .Max()!.Value;

        if (latest.Year > 9900)
        {
            throw new InvalidOperationException("test database dates near year 9999 - recreate the test database");
        }

        var day = latest.Date.AddDays(90 + RandomNumberGenerator.GetInt32(0, 30));
        var noon = new DateTime(day.Year, day.Month, day.Day, 12, 0, 0, DateTimeKind.Utc);
        return FixedClock(new DateTimeOffset(noon));
    }

    /// <summary>A clock that always returns <paramref name="utcNow"/>.</summary>
    public static TimeProvider FixedClock(DateTimeOffset utcNow)
    {
        return new FixedTimeProvider(utcNow);
    }

    private static string RandomBase36(int length)
    {
        return string.Create(length, 0, (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = Base36[RandomNumberGenerator.GetInt32(Base36.Length)];
            }
        });
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
