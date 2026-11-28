using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;

namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// Bounds the work-order list the MCP server reads for one call, so an anonymous <c>list-work-orders</c> cannot load
/// and serialize the whole table. The cap comes from <c>Pagination:MaxPageSize</c> (default
/// <see cref="DefaultMaxPageSize"/>); an omitted or non-positive take gets the cap and an oversize take is clamped to it.
/// Runs as a bus behavior, so the limit reaches SQL as <see cref="WorkOrderSpecificationQuery.Take"/>.
/// </summary>
public sealed class PaginationCapGuard(IConfiguration configuration)
    : IPipelineBehavior<WorkOrderSpecificationQuery, WorkOrder[]>
{
    /// <summary>Cap used when <c>Pagination:MaxPageSize</c> is missing or not positive.</summary>
    public const int DefaultMaxPageSize = 100;

    /// <summary>Configuration key holding the largest page a list call may return.</summary>
    public const string MaxPageSizeKey = "Pagination:MaxPageSize";

    /// <summary>Largest page a list call may return.</summary>
    public int MaxPageSize { get; } = configuration.GetValue<int?>(MaxPageSizeKey) is int configured and > 0
        ? configured
        : DefaultMaxPageSize;

    /// <summary>The page size to read: <see cref="MaxPageSize"/> when omitted or not positive, else at most it.</summary>
    public int Resolve(int? requested) =>
        requested is int take and > 0 ? Math.Min(take, MaxPageSize) : MaxPageSize;

    /// <inheritdoc />
    public Task<WorkOrder[]> Handle(WorkOrderSpecificationQuery request, RequestHandlerDelegate<WorkOrder[]> next,
        CancellationToken cancellationToken)
    {
        request.Take = Resolve(request.Take);
        return next();
    }
}
