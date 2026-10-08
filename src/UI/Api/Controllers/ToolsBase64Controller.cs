using System.Net.Mime;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Encodes and decodes Base64 text for operators and integrations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/tools/base64")]
[Route($"{ApiRoutes.VersionedApiPrefix}/tools/base64")]
[EnableRateLimiting(ApiRateLimiting.PolicyName)]
public class ToolsBase64Controller : ControllerBase
{
    /// <summary>
    /// Encodes or decodes <paramref name="request"/>.Text as Base64 (UTF-8).
    /// When <c>mode</c> is omitted or null, defaults to encode.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(Base64Response), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Post([FromBody] Base64Request? request)
    {
        if (request?.Text is null || request.Text.Length == 0)
        {
            return Problem(
                detail: "JSON body field 'text' is required and must be non-empty.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var mode = string.IsNullOrWhiteSpace(request.Mode)
            ? "encode"
            : request.Mode.Trim().ToLowerInvariant();

        return mode switch
        {
            "encode" => Ok(new Base64Response(
                Convert.ToBase64String(Encoding.UTF8.GetBytes(request.Text)))),
            "decode" => Decode(request.Text),
            _ => Problem(
                detail: "JSON body field 'mode' must be 'encode' or 'decode'.",
                statusCode: StatusCodes.Status400BadRequest)
        };
    }

    private IActionResult Decode(string text)
    {
        try
        {
            var bytes = Convert.FromBase64String(text);
            return Ok(new Base64Response(Encoding.UTF8.GetString(bytes)));
        }
        catch (FormatException)
        {
            return Problem(
                detail: "JSON body field 'text' is not valid Base64.",
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}

/// <summary>
/// Request body for <c>POST /api/tools/base64</c>.
/// </summary>
/// <param name="Text">Plain text to encode, or Base64 to decode.</param>
/// <param name="Mode"><c>encode</c> (default) or <c>decode</c>; case-insensitive.</param>
public record Base64Request(string? Text, string? Mode = null);

/// <summary>
/// JSON payload for <c>POST /api/tools/base64</c>.
/// </summary>
/// <param name="Result">Encoded Base64 string or decoded UTF-8 text.</param>
public record Base64Response(string Result);
