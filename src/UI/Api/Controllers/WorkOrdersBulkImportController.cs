using System.Net.Mime;
using Asp.Versioning;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Import;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Accepts CSV uploads to create multiple draft work orders in one request.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/work-orders/bulk-import")]
[Route($"{ApiRoutes.VersionedApiPrefix}/work-orders/bulk-import")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public sealed class WorkOrdersBulkImportController(
    IBus bus,
    IWorkOrderNumberGenerator numberGenerator) : ControllerBase
{
    /// <summary>
    /// Imports work orders from a CSV file. Form field name: <c>file</c>.
    /// Header row required: Title, Description, CreatorUsername, RoomNumber (optional).
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(WorkOrderBulkImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post(IFormFile file, CancellationToken cancellationToken)
    {
        if (WorkOrderBulkImportProcessor.ValidateUpload(file) is { } uploadError)
        {
            return BadRequestProblem(uploadError);
        }

        await using var stream = file.OpenReadStream();
        var parseResult = WorkOrderBulkImportCsvParser.Parse(stream, cancellationToken);
        if (RowsProblem(parseResult) is { } rowsError)
        {
            return BadRequestProblem(rowsError);
        }

        var processor = new WorkOrderBulkImportProcessor(bus, numberGenerator);
        return Ok(await processor.ImportAsync(parseResult.Rows, cancellationToken));
    }

    /// <summary>Why the CSV cannot be imported: it did not parse, or it has a header and no data rows.</summary>
    private static string? RowsProblem(WorkOrderBulkImportParseResult parseResult) => parseResult switch
    {
        { Success: false } => parseResult.Error,
        { Rows.Count: 0 } => "CSV contains no data rows.",
        _ => null
    };

    private ObjectResult BadRequestProblem(string? detail) =>
        Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);
}
