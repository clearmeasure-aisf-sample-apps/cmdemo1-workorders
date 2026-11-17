using System.Globalization;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

/// <summary>
/// The JSON the work-order MCP tools answer with: an indented summary per work order in lists, the full detail for a
/// single work order, and attachment metadata.
/// </summary>
internal static class WorkOrderJson
{
    public static string Summaries(IEnumerable<WorkOrder> workOrders) =>
        JsonSerializer.Serialize(workOrders.Select(Summary).ToArray(), McpJsonContext.Default.WorkOrderSummaryJsonArray);

    public static string Detail(WorkOrder workOrder) => JsonSerializer.Serialize(new WorkOrderDetailJson
    {
        Number = workOrder.Number,
        Title = workOrder.Title,
        Description = workOrder.Description,
        Instructions = workOrder.Instructions ?? "",
        Status = workOrder.Status.FriendlyName,
        RoomNumber = workOrder.RoomNumber,
        Creator = workOrder.Creator?.GetFullName(),
        CreatorUsername = workOrder.Creator?.UserName,
        Assignee = workOrder.Assignee?.GetFullName(),
        AssigneeUsername = workOrder.Assignee?.UserName,
        CreatedDate = workOrder.CreatedDate,
        AssignedDate = workOrder.AssignedDate,
        CompletedDate = workOrder.CompletedDate,
        DueDate = FormatDueDate(workOrder),
        Priority = workOrder.Priority.ToString(),
        Building = workOrder.Building
    }, McpJsonContext.Default.WorkOrderDetailJson);

    public static string Attachments(IEnumerable<WorkOrderAttachment> attachments) =>
        JsonSerializer.Serialize(attachments.Select(attachment => new WorkOrderAttachmentJson
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            FileSize = attachment.FileSize,
            UploadedBy = attachment.UploadedBy?.GetFullName(),
            UploadedByUsername = attachment.UploadedBy?.UserName,
            UploadedDate = attachment.UploadedDate
        }).ToArray(), McpJsonContext.Default.WorkOrderAttachmentJsonArray);

    private static WorkOrderSummaryJson Summary(WorkOrder workOrder) => new()
    {
        Number = workOrder.Number,
        Title = workOrder.Title,
        Status = workOrder.Status.FriendlyName,
        Creator = workOrder.Creator?.GetFullName(),
        Assignee = workOrder.Assignee?.GetFullName(),
        DueDate = FormatDueDate(workOrder),
        Priority = workOrder.Priority.ToString(),
        Building = workOrder.Building
    };

    private static string? FormatDueDate(WorkOrder workOrder) =>
        workOrder.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
