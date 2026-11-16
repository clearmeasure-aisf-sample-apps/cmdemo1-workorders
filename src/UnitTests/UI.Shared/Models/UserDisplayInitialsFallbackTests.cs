using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class UserDisplayInitialsFallbackTests
{
    [TestCase("homer.simpson", "HS")]
    [TestCase("bart_simpson", "BS")]
    [TestCase("lisa-marie", "LM")]
    [TestCase("maggie", "MA")]
    [TestCase("", "?")]
    public void InitialsFromName_UsesFirstTwoParts(string name, string expected)
    {
        UserDisplay.Initials(null, name).ShouldBe(expected);
    }
}
