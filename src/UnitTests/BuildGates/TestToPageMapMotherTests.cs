using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class TestToPageMapMotherTests
{
    [Test]
    public void LoginLandingAndTeams_WhenBuilt_IncludesLoginAndTeamCards()
    {
        var map = TestToPageMapMother.LoginLandingAndTeams();

        map.Tests.Select(entry => entry.Class).ShouldContain(
            "ClearMeasure.Bootcamp.AcceptanceTests.Authentication.LoginTests");
        map.Components.Select(entry => entry.Path).ShouldContain(
            "src/UI.Shared/Components/Teams/TeamCards.razor");
    }
}
