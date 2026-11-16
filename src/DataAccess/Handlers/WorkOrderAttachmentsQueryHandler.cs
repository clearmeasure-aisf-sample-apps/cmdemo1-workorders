using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderAttachmentsQueryHandler(DataContext context)
    : IRequestHandler<WorkOrderAttachmentsQuery, WorkOrderAttachment[]>
{
    private readonly DataContext context = context;

    public async ValueTask<WorkOrderAttachment[]> Handle(WorkOrderAttachmentsQuery request,
        CancellationToken cancellationToken = default)
    {
        var workOrderId = request.WorkOrderId;
        var ct = cancellationToken;
        return await context.Set<WorkOrderAttachment>()
            .Include(a => a.UploadedBy)
            .Where(a => a.WorkOrderId == workOrderId)
            .OrderBy(a => a.UploadedDate)
            .ToArrayAsync(ct);
    }
}
