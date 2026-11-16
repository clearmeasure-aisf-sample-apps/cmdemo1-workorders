using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>Deletes one attachment row when <see cref="RemoveAttachmentCommand.Validate"/> allows it.</summary>
public class RemoveAttachmentCommandHandler(DataContext context)
    : IRequestHandler<RemoveAttachmentCommand, RemoveAttachmentResult>
{
    private readonly DataContext context = context;

    public async ValueTask<RemoveAttachmentResult> Handle(RemoveAttachmentCommand request,
        CancellationToken cancellationToken = default)
    {
        var workOrderId = request.WorkOrderId;
        var userName = request.UserName;
        var attachmentId = request.AttachmentId;
        var ct = cancellationToken;
        var workOrder = await context.Set<WorkOrder>().AsNoTracking()
            .Include(w => w.Creator).Include(w => w.Assignee)
            .SingleOrDefaultAsync(w => w.Id == workOrderId, ct);
        var user = await context.Set<Employee>().AsNoTracking()
            .SingleOrDefaultAsync(e => e.UserName == userName, ct);
        var attachment = await context.Set<WorkOrderAttachment>()
            .SingleOrDefaultAsync(a => a.Id == attachmentId, ct);

        var error = RemoveAttachmentCommand.Validate(workOrder, user, attachment);
        if (error is not null) return RemoveAttachmentResult.Failure(error);

        context.Remove(attachment!);
        await context.SaveChangesAsync(cancellationToken);
        return RemoveAttachmentResult.Success;
    }
}
