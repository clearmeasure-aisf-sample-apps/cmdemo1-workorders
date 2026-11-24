using System.Collections.Concurrent;
using System.Globalization;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UnitTests.Acceptance;

/// <summary>
/// Last name for an acceptance employee that must appear in a dropdown fed by <see cref="EmployeeGetAllQuery"/>,
/// which returns only the first <see cref="EmployeeGetAllQuery.MaxTake"/> employees by last name (#2006).
/// Shared environments (tdd) keep the employees of earlier runs, named <c>!&lt;test tag&gt;</c>; once more than
/// a page of them piled up, a new employee with a random early-sorting name fell off the page (#2004).
/// This name starts with <c>!!</c>, which sorts before every <c>!&lt;hex tag&gt;</c> leftover, then a key that
/// counts down with time, so the newest employee sorts first. The test tag ends it to keep it unique.
/// </summary>
public static class LoginDirectoryLastName
{
    private const string Prefix = "!!";
    private const long LastMillisecond = 9_999_999_999_999;
    private static readonly ConcurrentDictionary<string, string> ByTestTag = new();

    /// <summary>
    /// The last name for <paramref name="testTag"/>, made on first use and returned unchanged on every later call,
    /// so a fixture can seed with it and assert against it.
    /// </summary>
    public static string For(string testTag) =>
        ByTestTag.GetOrAdd(testTag, tag => Create(tag, TimeProvider.System.GetUtcNow()));

    /// <summary>The last name for an employee tagged <paramref name="testTag"/> and created at <paramref name="createdAt"/>.</summary>
    public static string Create(string testTag, DateTimeOffset createdAt)
    {
        var countdown = LastMillisecond - createdAt.ToUnixTimeMilliseconds();
        return Prefix + countdown.ToString("D13", CultureInfo.InvariantCulture) + testTag;
    }
}
