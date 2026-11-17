using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class EmployeeQueryHandler(DataContext context)
    : IRequestHandler<EmployeeByUserNameQuery, Employee>,
        IRequestHandler<EmployeeGetAllQuery, Employee[]>
{
    // A field, not the primary-constructor parameter, so EF 11 can precompile the queries for Native AOT.
    // ReSharper disable once ReplaceWithPrimaryConstructorParameter
    private readonly DataContext _context = context;

    public async ValueTask<Employee> Handle(EmployeeByUserNameQuery request,
        CancellationToken cancellationToken = default)
    {
        var userName = request.Username;
        var ct = cancellationToken;
        var employee = await _context.Set<Employee>()
            .Include("Roles")
            .SingleAsync(emp => emp.UserName == userName, ct);
        return employee;
    }

    public async ValueTask<Employee[]> Handle(EmployeeGetAllQuery request, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(request.Take, 1, EmployeeGetAllQuery.MaxTake);
        var skip = Math.Max(request.Skip, 0);
        var ct = cancellationToken;

        // One statement: EF applies Take to the employees in a subquery before joining their roles. The database
        // returns the first skip + take employees and the skip happens here: with both Skip and Take, EF 11's
        // precompiled (Native AOT) query names both SQL parameters @p and the page comes back empty.
        var end = skip + take;
        // ReSharper disable DefaultStringComparerUsage -- EF Core translates this ordering to SQL, where the column collation decides; a comparer cannot be translated.
        var firstEmployees = await _context.Set<Employee>()
            .AsNoTracking()
            .Include(employee => employee.Roles)
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ThenBy(employee => employee.UserName)
            .Take(end)
            .ToListAsync(ct);
        // ReSharper restore DefaultStringComparerUsage
        var employees = firstEmployees.Skip(skip);
        return employees.OrderBy(employee => employee.LastName, StringComparer.CurrentCulture)
            .ThenBy(employee => employee.FirstName, StringComparer.CurrentCulture).ToArray();
    }
}