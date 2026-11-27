using System.Diagnostics.CodeAnalysis;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Import;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Validates CSV uploads and imports parsed rows as draft work orders.
/// </summary>
internal sealed class WorkOrderBulkImportProcessor(IBus bus, IWorkOrderNumberGenerator numberGenerator)
{
    private static readonly string RequiredFieldsMessage =
        $"{WorkOrderBulkImportColumns.Title}, {WorkOrderBulkImportColumns.Description}, and {WorkOrderBulkImportColumns.CreatorUsername} are required on each data row.";

    internal static string? ValidateUpload(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return "A non-empty CSV file is required (form field name: file).";
        }

        if (!IsCsvFile(file))
        {
            return "Upload must be a .csv file.";
        }

        return null;
    }

    internal static bool IsCsvFile(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName);
        return string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase)
               || string.Equals(file.ContentType, "text/csv", StringComparison.OrdinalIgnoreCase)
               || string.Equals(file.ContentType, "application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase);
    }

    internal async Task<WorkOrderBulkImportResponse> ImportAsync(
        IReadOnlyList<WorkOrderBulkImportRow> rows,
        CancellationToken cancellationToken)
    {
        var results = new List<WorkOrderBulkImportRowResult>(rows.Count);
        var created = 0;
        var creatorsByUsername = new Dictionary<string, Employee>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rowResult = await ImportRowAsync(row, creatorsByUsername, cancellationToken);
            if (rowResult.Success)
            {
                created++;
            }

            results.Add(rowResult);
        }

        return new WorkOrderBulkImportResponse(created, results);
    }

    private async Task<WorkOrderBulkImportRowResult> ImportRowAsync(
        WorkOrderBulkImportRow row,
        Dictionary<string, Employee> creatorsByUsername,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredFields(row, out var creatorUsername))
        {
            return RowFailure(row, RequiredFieldsMessage);
        }

        if (row.Error != null)
        {
            return RowFailure(row, row.Error);
        }

        var lookup = await ResolveCreatorAsync(creatorUsername, creatorsByUsername, cancellationToken);
        if (!lookup.IsFound)
        {
            return RowFailure(row, lookup.Error);
        }

        return await SaveDraftAsync(row, lookup.Creator, cancellationToken);
    }

    private static WorkOrderBulkImportRowResult RowFailure(WorkOrderBulkImportRow row, string? error) =>
        new(row.LineNumber, false, null, error);

    private static bool HasRequiredFields(
        WorkOrderBulkImportRow row,
        [NotNullWhen(true)] out string? creatorUsername)
    {
        creatorUsername = row.CreatorUsername;
        return !string.IsNullOrWhiteSpace(row.Title)
               && !string.IsNullOrWhiteSpace(row.Description)
               && !string.IsNullOrWhiteSpace(creatorUsername);
    }

    private async Task<CreatorLookup> ResolveCreatorAsync(
        string username,
        Dictionary<string, Employee> creatorsByUsername,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (creatorsByUsername.TryGetValue(username, out var cached))
        {
            return CreatorLookup.Found(cached);
        }

        try
        {
            var creator = await bus.Send(new EmployeeByUserNameQuery(username));
            creatorsByUsername[username] = creator;
            return CreatorLookup.Found(creator);
        }
        catch (InvalidOperationException)
        {
            return CreatorLookup.NotFound($"Employee with username '{username}' was not found.");
        }
    }

    private async Task<WorkOrderBulkImportRowResult> SaveDraftAsync(
        WorkOrderBulkImportRow row,
        Employee creator,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workOrder = new WorkOrder
        {
            Title = row.Title,
            Description = row.Description,
            Creator = creator,
            Status = WorkOrderStatus.Draft,
            Number = numberGenerator.GenerateNumber(),
            RoomNumber = row.RoomNumber,
            Building = row.Building,
            Priority = row.Priority ?? WorkOrderPriority.Medium,
            DueDate = row.DueDate
        };

        try
        {
            var saveResult = await bus.Send(new SaveDraftCommand(workOrder, creator));
            return new WorkOrderBulkImportRowResult(row.LineNumber, true, saveResult.WorkOrder.Number, null);
        }
        catch (Exception ex)
        {
            return new WorkOrderBulkImportRowResult(row.LineNumber, false, null, ex.Message);
        }
    }

    /// <summary>
    /// Outcome of resolving a row's creator: either the employee (<see cref="IsFound"/>) or a not-found error.
    /// </summary>
    private readonly record struct CreatorLookup(Employee? Creator, string? Error)
    {
        [MemberNotNullWhen(true, nameof(Creator))]
        public bool IsFound => Creator is not null;

        public static CreatorLookup Found(Employee creator) => new(creator, null);

        public static CreatorLookup NotFound(string error) => new(null, error);
    }
}
