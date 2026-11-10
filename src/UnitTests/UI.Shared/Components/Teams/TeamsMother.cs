using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Teams;

/// <summary>Employees with load, teams and vendors for the Teams & Vendors component fixtures.</summary>
public static class TeamsMother
{
    public static EmployeeWorkLoadSummary Loaded(string userName, string lastName, int assignedAndOpen) => new()
    {
        Employee = new Employee(userName, userName, lastName, $"{userName}@example.com"),
        Load = new EmployeeWorkLoad { AssignedAndOpen = assignedAndOpen }
    };

    public static Team MaintenanceTeamWith(Employee member)
    {
        var team = new Team("Maintenance", "Building upkeep") { Id = Guid.NewGuid() };
        team.AddMember(member);
        return team;
    }

    public static Vendor InactiveVendor() => new() { Id = Guid.NewGuid(), Name = "Acme Plumbing", IsActive = false };
}
