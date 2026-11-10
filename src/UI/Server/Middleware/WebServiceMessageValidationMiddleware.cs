using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Messaging;
using FluentValidation;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Validates a Blazor WASM single-API POST before MVC: the body must be a <see cref="WebServiceMessage"/>, the
/// envelope must pass its validator, and the request it carries must pass the validator of its type.
/// </summary>
public sealed class WebServiceMessageValidationMiddleware(
    RequestDelegate next,
    ILogger<WebServiceMessageValidationMiddleware> logger)
{
    private const string SingleApiPathSuffix = "/blazor-wasm-single-api";
    private const string InvalidBody = "Invalid request body.";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            IncludeFields = false,
            PropertyNameCaseInsensitive = true
        };

    public async Task InvokeAsync(
        HttpContext context,
        IValidator<WebServiceMessage> envelopeValidator,
        IServiceProvider services)
    {
        if (!IsBlazorWasmSingleApiPost(context.Request))
        {
            await next(context);
            return;
        }

        var body = await ReadRequestBodyAsync(context.Request);
        if (!TryDeserializeMessage(body, out var message, out var deserializeError))
        {
            logger.LogDebug("Invalid WebServiceMessage JSON: {Detail}", deserializeError);
            await WriteBadRequestAsync(context, deserializeError ?? InvalidBody);
            return;
        }

        if (await IsValidAsync(context, message!, envelopeValidator, services))
        {
            await next(context);
        }
    }

    internal static bool IsBlazorWasmSingleApiPost(HttpRequest request) =>
        HttpMethods.IsPost(request.Method)
        && (request.Path.Value ?? "").EndsWith(SingleApiPathSuffix, StringComparison.OrdinalIgnoreCase);

    internal static bool TryDeserializeMessage(string body, out WebServiceMessage? message, out string? errorDetail)
    {
        try
        {
            message = JsonSerializer.Deserialize<WebServiceMessage>(body, JsonOptions);
        }
        catch (JsonException)
        {
            message = null;
        }

        errorDetail = message is null ? InvalidBody : null;
        return message is not null;
    }

    /// <summary>Validates the envelope, then the payload; writes the 400 and answers false when either fails.</summary>
    private static async Task<bool> IsValidAsync(
        HttpContext context,
        WebServiceMessage message,
        IValidator<WebServiceMessage> envelopeValidator,
        IServiceProvider services)
    {
        var envelopeResult = await envelopeValidator.ValidateAsync(message, context.RequestAborted);
        if (!envelopeResult.IsValid)
        {
            await ProblemDetailsWriter.WriteValidationProblemAsync(context, envelopeResult.Errors);
            return false;
        }

        var payloadResult = await WebServiceMessagePayloadValidator.ValidateAsync(message, services, context.RequestAborted);
        if (payloadResult.IsValid)
        {
            return true;
        }

        await (payloadResult.Errors.Count > 0
            ? ProblemDetailsWriter.WriteValidationProblemAsync(context, payloadResult.Errors)
            : WriteBadRequestAsync(context, payloadResult.BadRequestDetail ?? InvalidBody));
        return false;
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(request.HttpContext.RequestAborted);
        request.Body.Position = 0;
        return body;
    }

    private static Task WriteBadRequestAsync(HttpContext context, string detail) =>
        ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status400BadRequest, "Bad Request", detail);
}
