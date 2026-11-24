using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Acceptance;

/// <summary>
/// Ordinal order matches SQL Server's for these ASCII names: <c>!</c> before digits before letters.
/// The integration test <c>EmployeeQueryHandlerTests</c> proves the same order through the real query.
/// </summary>
[TestFixture]
public class LoginDirectoryLastNameTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 24, 21, 0, 0, TimeSpan.Zero);

    [Test]
    public void Create_ShouldSortBeforeEveryHexTaggedLeftover_WhenComparedOrdinally()
    {
        var name = LoginDirectoryLastName.Create("ffffffff", CreatedAt);

        string.CompareOrdinal(name, "!00000000").ShouldBeLessThan(0);
        string.CompareOrdinal(name, "!0001b8e3").ShouldBeLessThan(0);
        string.CompareOrdinal(name, "!ffffffff").ShouldBeLessThan(0);
        string.CompareOrdinal(name, "Lovejoy").ShouldBeLessThan(0);
    }

    [Test]
    public void Create_ShouldSortTheNewerEmployeeFirst_WhenCreatedOneMillisecondLater()
    {
        var older = LoginDirectoryLastName.Create("00000000", CreatedAt);
        var newer = LoginDirectoryLastName.Create("ffffffff", CreatedAt.AddMilliseconds(1));

        string.CompareOrdinal(newer, older).ShouldBeLessThan(0);
    }

    [Test]
    public void Create_ShouldSortTheNewerEmployeeFirst_WhenTheCountdownLosesADigitPlace()
    {
        var older = LoginDirectoryLastName.Create("abc", DateTimeOffset.FromUnixTimeMilliseconds(8_999_999_999_999));
        var newer = LoginDirectoryLastName.Create("abc", DateTimeOffset.FromUnixTimeMilliseconds(9_000_000_000_000));

        string.CompareOrdinal(newer, older).ShouldBeLessThan(0);
        newer.Length.ShouldBe(older.Length);
    }

    [Test]
    public void Create_ShouldStartWithTwoBangsAndEndWithTheTestTag()
    {
        var name = LoginDirectoryLastName.Create("1a2b3c4d", CreatedAt);

        name.ShouldStartWith("!!");
        name.ShouldEndWith("1a2b3c4d");
        name.Length.ShouldBe(2 + 13 + 8);
    }

    [Test]
    public void For_ShouldReturnTheSameName_WhenCalledAgainForTheSameTag()
    {
        var tag = Guid.NewGuid().ToString("N")[..8];

        var first = LoginDirectoryLastName.For(tag);
        var second = LoginDirectoryLastName.For(tag);

        second.ShouldBe(first);
        first.ShouldEndWith(tag);
    }

    [Test]
    public void For_ShouldSortAheadOfAnOlderRunsName_WhenMadeNow()
    {
        var olderRun = LoginDirectoryLastName.Create("00000000", TimeProvider.System.GetUtcNow().AddMinutes(-1));

        var current = LoginDirectoryLastName.For(Guid.NewGuid().ToString("N")[..8]);

        string.CompareOrdinal(current, olderRun).ShouldBeLessThan(0);
    }
}
