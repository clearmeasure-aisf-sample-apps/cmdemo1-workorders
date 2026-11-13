using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.AcceptanceTests.TestUsers;

/// <summary>
/// Legacy SQL helpers for the #1638 leftover-employee sweep tests.
/// Browser fixtures create employees through the test-support HTTP API instead
/// (<see cref="TestSupport.TestSupportClient"/>), so they do not depend on SQL from the runner.
/// </summary>
public static class AcceptanceTestEmployees
{
    /// <summary>Starts the user name of employees created for browser acceptance tests.</summary>
    public const string UserNamePrefix = "test_";

    /// <summary>
    /// Deletes, with their role links and saved searches, the employees whose user name starts with
    /// <paramref name="userNamePrefix"/> and that no work order, status change, attachment or team refers to.
    /// Returns how many employees it deleted.
    /// </summary>
    public static int DeleteUnreferenced(DbContext context, string userNamePrefix)
    {
        var unreferenced = Unreferenced(context, userNamePrefix);
        using var transaction = context.Database.BeginTransaction();
        context.Set<Dictionary<string, object>>("EmployeeRoles")
            .Where(link => unreferenced.Any(employee => employee.Id == EF.Property<Guid>(link, "EmployeeId")))
            .ExecuteDelete();
        context.Set<SavedSearchView>()
            .Where(view => unreferenced.Any(employee => employee.Id == view.EmployeeId))
            .ExecuteDelete();
        var deleted = unreferenced.ExecuteDelete();
        transaction.Commit();
        return deleted;
    }

    private static IQueryable<Employee> Unreferenced(DbContext context, string userNamePrefix) =>
        context.Set<Employee>().Where(employee =>
            employee.UserName.StartsWith(userNamePrefix)
            && !context.Set<WorkOrder>().Any(order =>
                order.Creator!.Id == employee.Id || order.Assignee!.Id == employee.Id)
            && !context.Set<WorkOrderStatusChange>().Any(change => change.ChangedById == employee.Id)
            && !context.Set<WorkOrderAttachment>().Any(attachment => attachment.UploadedById == employee.Id)
            && !context.Set<Team>().Any(team => team.Members.Any(member => member.Id == employee.Id)));
}
