using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Loads a work order by number, applies reformatted fields, and saves through EF Core.
/// </summary>
public class ReformatWorkOrderCommandHandler(DbContext dbContext)
    : IRequestHandler<ReformatWorkOrderCommand, WorkOrder?>
{
    private readonly DataContext dbContext = (DataContext)dbContext;

    public async ValueTask<WorkOrder?> Handle(ReformatWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var number = request.Number;
        var ct = cancellationToken;
        var workOrder = await dbContext.Set<WorkOrder>()
            .SingleOrDefaultAsync(order => order.Number == number, ct);
        if (workOrder is null)
        {
            return null;
        }

        workOrder.ApplyReformat(request.Title, request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return workOrder;
    }
}
