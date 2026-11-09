using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class EmployeeQueryHandler(DataContext context)
    : IRequestHandler<EmployeeByUserNameQuery, Employee>,
        IRequestHandler<EmployeeGetAllQuery, Employee[]>
{
    public async Task<Employee> Handle(EmployeeByUserNameQuery request,
        CancellationToken cancellationToken = default)
    {
        var employee = await context.Set<Employee>()
            .Include("Roles")
            .SingleAsync(emp => emp.UserName == request.Username, cancellationToken);
        return employee;
    }

    public async Task<Employee[]> Handle(EmployeeGetAllQuery request, CancellationToken cancellationToken = default)
    {
        int take = Math.Clamp(request.Take, 1, EmployeeGetAllQuery.MaxTake);
        int skip = Math.Max(request.Skip, 0);

        // Page the keys first. Including Roles in the same statement would make Skip/Take
        // apply to the join rows instead of employees.
        var ids = await context.Set<Employee>()
            .AsNoTracking()
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ThenBy(employee => employee.UserName)
            .Skip(skip)
            .Take(take)
            .Select(employee => employee.Id)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
        {
            return [];
        }

        var employees = await context.Set<Employee>()
            .AsNoTracking()
            .Include(employee => employee.Roles)
            .Where(employee => ids.Contains(employee.Id))
            .ToListAsync(cancellationToken);

        return employees.OrderBy(employee => employee.LastName).ThenBy(employee => employee.FirstName).ToArray();
    }
}
