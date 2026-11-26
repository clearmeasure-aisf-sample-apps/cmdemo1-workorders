using System.Security.Cryptography;

namespace ClearMeasure.Bootcamp.UnitTests.TestSupport;

/// <summary>
/// The single source of unique work order numbers for test rows, shared by <see cref="WorkOrderBuilder"/>
/// and the integration-test data helpers. Numbers are seven uppercase base36 characters, so they fit the
/// work order number column and do not collide with rows other tests created.
/// </summary>
public static class UniqueWorkOrderNumber
{
    /// <summary>The length of every generated work order number.</summary>
    public const int Length = 7;

    private const string Base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int SeriesStemLength = 5;

    /// <summary>
    /// A unique work order number of <see cref="Length"/> characters: the optional one-character
    /// <paramref name="prefix"/> followed by random uppercase base36 characters.
    /// </summary>
    public static string Next(string prefix = "")
    {
        if (prefix.Length > 1)
        {
            throw new ArgumentException("A work order number prefix is at most one character.", nameof(prefix));
        }

        return prefix + RandomBase36(Length - prefix.Length);
    }

    /// <summary>
    /// <paramref name="count"/> unique work order numbers that share a random five-character stem
    /// and end in 01, 02, ...; they sort ordinally in index order.
    /// </summary>
    public static string[] Series(int count)
    {
        if (count is < 1 or > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "A number series has 1 to 99 numbers.");
        }

        var stem = RandomBase36(SeriesStemLength);
        return Enumerable.Range(1, count).Select(i => $"{stem}{i:00}").ToArray();
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
}
