using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Acceptance;

[TestFixture]
public class DropdownOptionTests
{
    [Test]
    public void Selector_ShouldMatchTheOptionValue_WhenValueIsAUserName()
    {
        DropdownOption.Selector("test_1a2b3c4d_Kelsi65").ShouldBe("option[value='test_1a2b3c4d_Kelsi65']");
    }

    [Test]
    public void Selector_ShouldEscapeQuotesAndBackslashes_WhenValueContainsThem()
    {
        DropdownOption.Selector(@"o'brien\x").ShouldBe(@"option[value='o\'brien\\x']");
    }

    [Test]
    public void MissingMessage_ShouldNameTheValueSelectAndWait_WhenOptionNeverRenders()
    {
        var message = DropdownOption.MissingMessage("User", "test_1a2b3c4d_Kelsi65", 12, 90_000);

        message.ShouldContain("'test_1a2b3c4d_Kelsi65'");
        message.ShouldContain("'User'");
        message.ShouldContain("90000 ms");
        message.ShouldContain("rendered 12 options");
        message.ShouldContain("loaded without the value");
    }

    [Test]
    public void MissingMessage_ShouldBlameTheDirectoryCap_WhenListIsFull()
    {
        var message = DropdownOption.MissingMessage("User", "test_x", EmployeeGetAllQuery.MaxTake, 90_000);

        message.ShouldContain("EmployeeGetAllQuery.MaxTake (1000)");
        message.ShouldContain("#2006");
        message.ShouldContain(nameof(LoginDirectoryLastName));
    }

    [Test]
    public void MissingMessage_ShouldSayTheListNeverLoaded_WhenNoOptionsRendered()
    {
        var message = DropdownOption.MissingMessage("Assignee", "test_x", 0, 90_000);

        message.ShouldContain("never loaded");
    }
}
