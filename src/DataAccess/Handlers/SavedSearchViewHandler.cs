using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>Lists, saves and deletes a user's saved Work Order Search views.</summary>
public class SavedSearchViewHandler(DataContext context, TimeProvider time) :
    IRequestHandler<SavedSearchViewsQuery, SavedSearchView[]>,
    IRequestHandler<SaveSearchViewCommand, SavedSearchViewResult>,
    IRequestHandler<DeleteSearchViewCommand, SavedSearchViewResult>
{
    private const string UnknownUser = "User not found.";

    public async Task<SavedSearchView[]> Handle(SavedSearchViewsQuery request, CancellationToken cancellationToken = default)
    {
        var employee = await FindEmployee(request.UserName, cancellationToken);
        if (employee == null) return [];

        var views = await context.Set<SavedSearchView>().AsNoTracking()
            .Where(v => v.EmployeeId == employee.Id)
            .ToListAsync(cancellationToken);

        return views.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<SavedSearchViewResult> Handle(SaveSearchViewCommand request, CancellationToken cancellationToken = default)
    {
        var employee = await FindEmployee(request.UserName, cancellationToken);
        if (employee == null) return SavedSearchViewResult.Failure(UnknownUser);

        var existingNames = await context.Set<SavedSearchView>().AsNoTracking()
            .Where(v => v.EmployeeId == employee.Id)
            .Select(v => v.Name)
            .ToListAsync(cancellationToken);

        var error = SavedSearchView.ValidateNew(request.Name, request.Query, existingNames);
        if (error != null) return SavedSearchViewResult.Failure(error);

        context.Add(new SavedSearchView
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            Name = request.Name.Trim(),
            Query = request.Query,
            CreatedDate = time.GetUtcNow().DateTime
        });
        await context.SaveChangesAsync(cancellationToken);
        return SavedSearchViewResult.Success;
    }

    public async Task<SavedSearchViewResult> Handle(DeleteSearchViewCommand request, CancellationToken cancellationToken = default)
    {
        var employee = await FindEmployee(request.UserName, cancellationToken);
        if (employee == null) return SavedSearchViewResult.Failure(UnknownUser);

        var view = await context.Set<SavedSearchView>()
            .SingleOrDefaultAsync(v => v.Id == request.Id && v.EmployeeId == employee.Id, cancellationToken);
        if (view == null) return SavedSearchViewResult.Failure("View not found.");

        context.Remove(view);
        await context.SaveChangesAsync(cancellationToken);
        return SavedSearchViewResult.Success;
    }

    private Task<Employee?> FindEmployee(string userName, CancellationToken cancellationToken) =>
        context.Set<Employee>().AsNoTracking().SingleOrDefaultAsync(e => e.UserName == userName, cancellationToken);
}
