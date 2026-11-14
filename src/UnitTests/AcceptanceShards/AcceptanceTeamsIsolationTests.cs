using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

/// <summary>Team cleanup must stay TestTag/prefix-scoped — never a global wipe.</summary>
[TestFixture]
public class AcceptanceTeamsIsolationTests
{
    [Test]
    public void EmployeesSweep_WhenRead_TearsDownLeftoversIncludingTeams()
    {
        var text = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/TestUsers/AcceptanceTestEmployeesTests.cs");
        text.ShouldContain("[TearDown]");
        text.ShouldContain("DeleteAllLeftovers");
    }

    [Test]
    public void EmployeesSweep_WhenRead_DeletesOnlyTeamsNamedWithPrefix()
    {
        var text = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/TestUsers/AcceptanceTestEmployees.cs");
        text.ShouldContain("DeleteTeamsCreatedUnderPrefix");
        text.ShouldContain("team.Name.Contains(userNamePrefix)");
        text.ShouldNotContain("team.Members.Any(member => employeeIds.Contains(member.Id))");
    }

    [Test]
    public void AcceptanceTeams_WhenRead_DeletesOnlyTaggedTeamNames()
    {
        var text = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/TestUsers/AcceptanceTeams.cs");
        text.ShouldContain("DeleteCreatedWithTag");
        text.ShouldContain("team.Name.Contains(tag)");
        text.ShouldNotContain("ClearAll");
        text.ShouldNotContain("Set<Team>().ExecuteDelete()");
    }

    [Test]
    public void TeamsPageBase_WhenRead_TearsDownTaggedTeamsOnly()
    {
        var text = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/TeamsAndVendors/TeamsAndVendorsAcceptanceTestBase.cs");
        text.ShouldContain("[TearDown]");
        text.ShouldContain("DeleteCreatedWithTag(TestTag)");
        text.ShouldNotContain("NewDbContext");
    }

    [Test]
    public void AxeParallelFixture_WhenRead_DoesNotWipeTeams()
    {
        var text = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/App/AccessibilityAxeTests.cs");
        text.ShouldNotContain("AcceptanceTeams");
        text.ShouldNotContain("yield return \"teams\"");
    }
}
