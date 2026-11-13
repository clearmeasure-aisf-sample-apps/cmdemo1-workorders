using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.AcceptanceTests.TestUsers;

// Non-parallelizable: the tests share _prefix, and two sweeps scanning Employee at once can deadlock on SQL Server.
[TestFixture, NonParallelizable, Category("SerialLane")]
public class AcceptanceTestEmployeesTests
{
    // A prefix of its own per test, so the sweep never touches the test_ employees of the browser tests.
    private string _prefix = null!;

    [SetUp]
    public void SetUp()
    {
        _prefix = $"sweep{Guid.NewGuid():N}"[..17] + "_";
    }

    [Test]
    public void ShouldDeleteUnreferencedEmployees_WithTheirRoleLinksAndSavedSearches()
    {
        var unreferenced = LeftoverEmployeeMother.Unreferenced(_prefix + "plain");
        var withSavedSearch = LeftoverEmployeeMother.WithSavedSearch(_prefix + "search");

        var deleted = DeleteUnreferenced();

        deleted.ShouldBe(2);
        EmployeeExists(unreferenced).ShouldBeFalse();
        EmployeeExists(withSavedSearch).ShouldBeFalse();
        RoleLinkCount(unreferenced).ShouldBe(0);
        SavedSearchCount(withSavedSearch).ShouldBe(0);
    }

    [Test]
    public void ShouldKeepEmployees_ThatAWorkOrderNamesAsCreatorOrAssignee()
    {
        var creator = LeftoverEmployeeMother.WorkOrderCreator(_prefix + "creator");
        var assignee = LeftoverEmployeeMother.WorkOrderAssignee(_prefix + "assignee", creator);

        var deleted = DeleteUnreferenced();

        deleted.ShouldBe(0);
        EmployeeExists(creator).ShouldBeTrue();
        EmployeeExists(assignee).ShouldBeTrue();
        RoleLinkCount(assignee).ShouldBe(1);
    }

    [Test]
    public void ShouldKeepEmployees_ThatStatusHistoryAttachmentsOrTeamsReferTo()
    {
        var owner = LeftoverEmployeeMother.Unreferenced($"owner{_prefix}");
        var changer = LeftoverEmployeeMother.StatusChanger(_prefix + "changer", owner);
        var uploader = LeftoverEmployeeMother.AttachmentUploader(_prefix + "uploader", owner);
        var member = LeftoverEmployeeMother.TeamMember(_prefix + "member");

        var deleted = DeleteUnreferenced();

        deleted.ShouldBe(0);
        EmployeeExists(changer).ShouldBeTrue();
        EmployeeExists(uploader).ShouldBeTrue();
        EmployeeExists(member).ShouldBeTrue();
    }

    [Test]
    public void ShouldKeepEmployees_WhoseUserNameMatchesThePrefixOnlyAsALikePattern()
    {
        var lookalike = LeftoverEmployeeMother.Unreferenced(_prefix[..^1] + "x" + "lookalike");
        var outside = LeftoverEmployeeMother.Unreferenced("x" + _prefix + "outside");

        var deleted = DeleteUnreferenced();

        deleted.ShouldBe(0);
        EmployeeExists(lookalike).ShouldBeTrue();
        EmployeeExists(outside).ShouldBeTrue();
    }

    private int DeleteUnreferenced()
    {
        using var context = TestHost.NewDbContext();
        return AcceptanceTestEmployees.DeleteUnreferenced(context, _prefix);
    }

    private static bool EmployeeExists(Employee employee)
    {
        using var context = TestHost.NewDbContext();
        return context.Set<Employee>().Any(saved => saved.Id == employee.Id);
    }

    private static int RoleLinkCount(Employee employee)
    {
        using var context = TestHost.NewDbContext();
        return context.Set<Dictionary<string, object>>("EmployeeRoles")
            .Count(link => EF.Property<Guid>(link, "EmployeeId") == employee.Id);
    }

    private static int SavedSearchCount(Employee employee)
    {
        using var context = TestHost.NewDbContext();
        return context.Set<SavedSearchView>().Count(view => view.EmployeeId == employee.Id);
    }
}
