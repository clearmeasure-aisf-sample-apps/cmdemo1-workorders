using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceShards;

/// <summary>Employee leftover sweeps must not leave team member avatars for parallel axe.</summary>
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
    public void AcceptanceTeams_WhenRead_ClearsAllTeams()
    {
        var text = RepositoryCheckout.Current.ReadText(
            "src/AcceptanceTests/TestUsers/AcceptanceTeams.cs");
        text.ShouldContain("ExecuteDelete");
        text.ShouldContain("Set<Team>");
    }
}
