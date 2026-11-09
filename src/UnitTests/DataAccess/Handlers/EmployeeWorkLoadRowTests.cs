using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

[TestFixture]
public class EmployeeWorkLoadRowTests
{
    [Test]
    public void ShouldSummarizeEachEmployeeOnce_WhenAnEmployeeHasSeveralRoles()
    {
        var willie = Guid.NewGuid();
        var marge = Guid.NewGuid();
        EmployeeWorkLoadRow[] rows = [Row(willie, "Fulfiller", 3), Row(willie, "Viewer", 3), Row(marge, "Fulfiller", 1)];

        var summaries = EmployeeWorkLoadRow.ToSummaries(rows);

        summaries.Select(summary => summary.Employee.Id).ShouldBe([willie, marge]);
        summaries[0].Employee.Roles.Count.ShouldBe(2);
        summaries[0].Load.AssignedAndOpen.ShouldBe(3);
        summaries[0].Load.Overdue.ShouldBe(1);
        summaries[1].Load.AssignedAndOpen.ShouldBe(1);
    }

    private static EmployeeWorkLoadRow Row(Guid employeeId, string roleName, int open) => new()
    {
        Member = new EmployeeRoleRow
        {
            Id = employeeId,
            UserName = employeeId.ToString("N"),
            FirstName = "First",
            LastName = "Last",
            EmailAddress = "e@example.com",
            PreferredLanguage = "en-US",
            RoleId = Guid.NewGuid(),
            RoleName = roleName,
            RoleCanFulfill = true
        },
        AssignedAndOpen = open,
        InProgress = 0,
        Overdue = 1,
        CompletedLast30Days = 2
    };
}
