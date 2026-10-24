using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkOrderStatusHistoryQuery(Guid WorkOrderId) : IRequest<WorkOrderStatusChange[]>, IRemotableRequest;
