using ClearMeasure.Bootcamp.Core.Model;
using Mediator;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkOrderByNumberQuery(string Number) : IRequest<WorkOrder?>, IRemotableRequest;