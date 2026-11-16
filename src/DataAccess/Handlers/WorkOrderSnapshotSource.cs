using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// The precompiled reads behind <see cref="WorkOrderSnapshot"/> for the Native AOT handlers. The context is a field and
/// every captured value a local because EF 11's precompiler rejects primary-constructor contexts and mishandles method
/// parameters in a query (dotnet/efcore#35494).
/// </summary>
internal sealed class WorkOrderSnapshotSource(DataContext context, TimeProvider time)
{
    private readonly DataContext _context = context;
    private readonly TimeProvider _time = time;

    public async Task<WorkOrderSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        var ct = cancellationToken;
        var all = await _context.Set<WorkOrder>().AsNoTracking().ToArrayAsync(ct);
        return new WorkOrderSnapshot(all, _time);
    }

    /// <summary>The employee with <paramref name="userName"/>, compared by the database, or null (also for a blank name).</summary>
    public Task<Employee?> FindEmployeeAsync(string? userName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return Task.FromResult<Employee?>(null);
        }

        var name = userName;
        var ct = cancellationToken;
        return _context.Set<Employee>().AsNoTracking().SingleOrDefaultAsync(e => e.UserName == name, ct);
    }

    public Task<Employee[]> EmployeesWithRolesAsync(CancellationToken cancellationToken)
    {
        var ct = cancellationToken;
        return _context.Set<Employee>().AsNoTracking().Include(e => e.Roles).ToArrayAsync(ct);
    }
}
