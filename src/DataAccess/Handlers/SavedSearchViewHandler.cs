using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>Lists, saves and deletes a user's saved Work Order Search views.</summary>
public class SavedSearchViewHandler(DataContext context, TimeProvider time) :
    IRequestHandler<SavedSearchViewsQuery, SavedSearchView[]>,
    IRequestHandler<SaveSearchViewCommand, SavedSearchViewResult>,
    IRequestHandler<DeleteSearchViewCommand, SavedSearchViewResult>
{
    // A field, not the primary-constructor parameter, so EF 11 can precompile the queries for Native AOT.
    // ReSharper disable once ReplaceWithPrimaryConstructorParameter
    private readonly DataContext _context = context;
    private const string UnknownUser = "User not found.";

    public async ValueTask<SavedSearchView[]> Handle(SavedSearchViewsQuery request, CancellationToken cancellationToken = default)
    {
        var employee = await FindEmployee(request.UserName, cancellationToken);
        if (employee == null) return [];

        var employeeId = employee.Id;
        var ct = cancellationToken;
        var views = await _context.Set<SavedSearchView>().AsNoTracking()
            .Where(v => v.EmployeeId == employeeId)
            .ToListAsync(ct);

        return views.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async ValueTask<SavedSearchViewResult> Handle(SaveSearchViewCommand request, CancellationToken cancellationToken = default)
    {
        var employee = await FindEmployee(request.UserName, cancellationToken);
        if (employee == null) return SavedSearchViewResult.Failure(UnknownUser);

        var employeeId = employee.Id;
        var ct = cancellationToken;
        var existingNames = await _context.Set<SavedSearchView>().AsNoTracking()
            .Where(v => v.EmployeeId == employeeId)
            .Select(v => v.Name)
            .ToListAsync(ct);

        var error = SavedSearchView.ValidateNew(request.Name, request.Query, existingNames);
        if (error != null) return SavedSearchViewResult.Failure(error);

        _context.Add(new SavedSearchView
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            Name = request.Name.Trim(),
            Query = request.Query,
            CreatedDate = time.GetUtcNow().DateTime
        });
        await _context.SaveChangesAsync(cancellationToken);
        return SavedSearchViewResult.Success;
    }

    public async ValueTask<SavedSearchViewResult> Handle(DeleteSearchViewCommand request, CancellationToken cancellationToken = default)
    {
        var employee = await FindEmployee(request.UserName, cancellationToken);
        if (employee == null) return SavedSearchViewResult.Failure(UnknownUser);

        var viewId = request.Id;
        var employeeId = employee.Id;
        var ct = cancellationToken;
        var view = await _context.Set<SavedSearchView>()
            .SingleOrDefaultAsync(v => v.Id == viewId && v.EmployeeId == employeeId, ct);
        if (view == null) return SavedSearchViewResult.Failure("View not found.");

        _context.Remove(view);
        await _context.SaveChangesAsync(cancellationToken);
        return SavedSearchViewResult.Success;
    }

    private Task<Employee?> FindEmployee(string userName, CancellationToken cancellationToken)
    {
        var name = userName;
        var ct = cancellationToken;
        return _context.Set<Employee>().AsNoTracking().SingleOrDefaultAsync(e => e.UserName == name, ct);
    }
}
