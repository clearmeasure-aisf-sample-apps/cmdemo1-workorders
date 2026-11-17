using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderAttachmentsQueryHandler(DataContext context)
    : IRequestHandler<WorkOrderAttachmentsQuery, WorkOrderAttachment[]>
{
    // A field, not the primary-constructor parameter, so EF 11 can precompile the queries for Native AOT.
    // ReSharper disable once ReplaceWithPrimaryConstructorParameter
    private readonly DataContext _context = context;

    public async ValueTask<WorkOrderAttachment[]> Handle(WorkOrderAttachmentsQuery request,
        CancellationToken cancellationToken = default)
    {
        var workOrderId = request.WorkOrderId;
        var ct = cancellationToken;
        return await _context.Set<WorkOrderAttachment>()
            .Include(a => a.UploadedBy)
            .Where(a => a.WorkOrderId == workOrderId)
            .OrderBy(a => a.UploadedDate)
            .ToArrayAsync(ct);
    }
}
