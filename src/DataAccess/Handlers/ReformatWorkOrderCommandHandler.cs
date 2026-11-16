using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Loads a work order by number, applies reformatted fields, and saves through EF Core.
/// </summary>
public class ReformatWorkOrderCommandHandler(DbContext dbContext)
    : IRequestHandler<ReformatWorkOrderCommand, WorkOrder?>
{
    public async ValueTask<WorkOrder?> Handle(ReformatWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await dbContext.Set<WorkOrder>()
            .SingleOrDefaultAsync(order => order.Number == request.Number, cancellationToken);
        if (workOrder is null)
        {
            return null;
        }

        workOrder.ApplyReformat(request.Title, request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return workOrder;
    }
}
