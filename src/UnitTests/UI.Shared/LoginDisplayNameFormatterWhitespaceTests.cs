using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class LoginDisplayNameFormatterWhitespaceTests
{
    [Test]
    public void FormatForLoginDropdown_RepeatedSpaces_CollapsesToSingleSpace()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("homer   j  simpson")
            .ShouldBe("HOMER J SIMPSON");
    }

    [Test]
    public void FormatForLoginDropdown_TabBetweenNames_CollapsesToSingleSpace()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("homer\tsimpson")
            .ShouldBe("HOMER SIMPSON");
    }

    [Test]
    public void FormatForLoginDropdown_SingleSpacedName_Unchanged()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("homer simpson")
            .ShouldBe("HOMER SIMPSON");
    }
}
