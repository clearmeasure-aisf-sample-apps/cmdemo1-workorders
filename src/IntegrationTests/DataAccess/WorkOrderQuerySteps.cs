using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

/// <summary>
/// Persist and query steps for DataAccess handler tests. Creates no work orders or employees.
/// </summary>
public static class WorkOrderQuerySteps
{
    /// <summary>Saves <paramref name="creator"/> and <paramref name="orders"/>, tracking distinct people.</summary>
    public static async Task SaveOrdersAsync(Employee creator, params WorkOrder[] orders)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        var people = orders
            .SelectMany(order => new[] { order.Creator, order.Assignee })
            .Append(creator)
            .OfType<Employee>()
            .Distinct();
        foreach (var person in people)
        {
            TrackEmployee(context, person);
        }

        context.AddRange(orders.Cast<object>());
        await context.SaveChangesAsync();
    }

    /// <summary>Saves an employee and a list of work orders.</summary>
    public static async Task SaveOrdersAsync(Employee creator, IReadOnlyList<WorkOrder> orders) =>
        await SaveOrdersAsync(creator, orders.ToArray());

    /// <summary>Runs a work-order specification search.</summary>
    public static async Task<WorkOrder[]> SearchAsync(
        WorkOrderSpecificationQuery specification,
        TimeProvider? clock = null)
    {
        var handler = new WorkOrderSearchHandler(
            TestHost.GetRequiredService<DataContext>(),
            clock ?? TimeProvider.System);
        return await handler.Handle(specification);
    }

    private static void TrackEmployee(DbContext context, Employee employee)
    {
        if (context.ChangeTracker.Entries<Employee>().Any(entry => ReferenceEquals(entry.Entity, employee)))
        {
            return;
        }

        if (employee.Id != Guid.Empty &&
            context.Set<Employee>().AsNoTracking().Any(existing => existing.Id == employee.Id))
        {
            context.Attach(employee);
            return;
        }

        context.Add(employee);
    }
}
