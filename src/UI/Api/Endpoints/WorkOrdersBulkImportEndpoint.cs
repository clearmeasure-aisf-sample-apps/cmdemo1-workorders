using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Import;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>
/// Accepts CSV uploads to create multiple draft work orders in one request.
/// </summary>
public static class WorkOrdersBulkImportEndpoint
{
    /// <summary>Largest accepted request body: 10 MiB.</summary>
    public const long MaxRequestBodyBytes = 10 * 1024 * 1024;

    public static void Map(IEndpointRouteBuilder api) =>
        api.MapPost("work-orders/bulk-import", Post)
            .AllowAnonymous()
            .Accepts<IFormFile>("multipart/form-data")
            .WithMetadata(new RequestSizeLimit(MaxRequestBodyBytes))
            .DisableAntiforgery();

    /// <summary>
    /// Imports work orders from a CSV file. Form field name: <c>file</c>.
    /// Header row required: Title, Description, CreatorUsername, RoomNumber (optional).
    /// </summary>
    public static async Task<IResult> Post(
        IFormFile? file,
        IBus bus,
        IWorkOrderNumberGenerator numberGenerator,
        CancellationToken cancellationToken)
    {
        if (WorkOrderBulkImportProcessor.ValidateUpload(file) is { } uploadError)
        {
            return ApiProblems.BadRequest(uploadError);
        }

        await using var stream = file!.OpenReadStream();
        var parseResult = WorkOrderBulkImportCsvParser.Parse(stream, cancellationToken);
        if (RowsProblem(parseResult) is { } rowsError)
        {
            return ApiProblems.BadRequest(rowsError);
        }

        var processor = new WorkOrderBulkImportProcessor(bus, numberGenerator);
        return TypedResults.Ok(await processor.ImportAsync(parseResult.Rows, cancellationToken));
    }

    /// <summary>Why the CSV cannot be imported: it did not parse, or it has a header and no data rows.</summary>
    private static string? RowsProblem(WorkOrderBulkImportParseResult parseResult) => parseResult switch
    {
        { Success: false } => parseResult.Error,
        { Rows.Count: 0 } => "CSV contains no data rows.",
        _ => null
    };
}
