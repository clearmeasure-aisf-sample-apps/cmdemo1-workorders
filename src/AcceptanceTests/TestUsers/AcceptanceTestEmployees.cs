using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.AcceptanceTests.TestUsers;

/// <summary>
/// The employees an acceptance run adds to the database it tests: one per browser test, user name
/// <c>test_&lt;tag&gt;_...</c>. A deployed environment keeps its database between runs, and the login list shows only
/// the first <see cref="EmployeeGetAllQuery.MaxTake"/> employees by last name, so the ones earlier runs left behind
/// must not pile up past that page or a new test's employee is not listed.
/// </summary>
public static class AcceptanceTestEmployees
{
    /// <summary>Starts the user name of every employee <see cref="Add"/> creates.</summary>
    public const string UserNamePrefix = "test_";

    /// <summary>
    /// Adds an employee with the admin role whose user name carries <paramref name="testTag"/>.
    /// </summary>
    public static Employee Add(string testTag)
    {
        using var context = TestHost.NewDbContext();
        var employee = TestHost.Faker<Employee>();
        employee.UserName = $"{UserNamePrefix}{testTag}_{employee.UserName}";
        employee.AddRole(new Role("admin", true, true));
        context.Add(employee);
        context.SaveChanges();
        return employee;
    }

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
