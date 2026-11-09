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
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static string Summaries(IEnumerable<WorkOrder> workOrders) =>
        Serialize(workOrders.Select(Summary).ToArray());

    public static string Detail(WorkOrder workOrder) => Serialize(new
    {
        workOrder.Number,
        workOrder.Title,
        workOrder.Description,
        Instructions = workOrder.Instructions ?? "",
        Status = workOrder.Status.FriendlyName,
        workOrder.RoomNumber,
        Creator = workOrder.Creator?.GetFullName(),
        CreatorUsername = workOrder.Creator?.UserName,
        Assignee = workOrder.Assignee?.GetFullName(),
        AssigneeUsername = workOrder.Assignee?.UserName,
        workOrder.CreatedDate,
        workOrder.AssignedDate,
        workOrder.CompletedDate,
        DueDate = FormatDueDate(workOrder),
        Priority = workOrder.Priority.ToString(),
        workOrder.Building
    });

    public static string Attachments(IEnumerable<WorkOrderAttachment> attachments) =>
        Serialize(attachments.Select(attachment => new
        {
            attachment.Id,
            attachment.FileName,
            attachment.ContentType,
            attachment.FileSize,
            UploadedBy = attachment.UploadedBy?.GetFullName(),
            UploadedByUsername = attachment.UploadedBy?.UserName,
            attachment.UploadedDate
        }).ToArray());

    private static object Summary(WorkOrder workOrder) => new
    {
        workOrder.Number,
        workOrder.Title,
        Status = workOrder.Status.FriendlyName,
        Creator = workOrder.Creator?.GetFullName(),
        Assignee = workOrder.Assignee?.GetFullName(),
        DueDate = FormatDueDate(workOrder),
        Priority = workOrder.Priority.ToString(),
        workOrder.Building
    };

    private static string? FormatDueDate(WorkOrder workOrder) =>
        workOrder.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Serialize(object value) => JsonSerializer.Serialize(value, Indented);
}
