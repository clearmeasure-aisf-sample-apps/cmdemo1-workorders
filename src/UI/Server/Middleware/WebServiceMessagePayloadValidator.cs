using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Messaging;
using FluentValidation;
using FluentValidation.Results;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Validates the request a <see cref="WebServiceMessage"/> carries. The HTTP allow-list is the explicit
/// <see cref="IRemotableRequest"/> / <see cref="IRemotableEvent"/> markers; when a FluentValidation
/// <see cref="IValidator"/> is registered for the payload type it runs via the non-generic API.
/// </summary>
internal static class WebServiceMessagePayloadValidator
{
    internal sealed record PayloadValidationResult(
        bool IsValid,
        IList<ValidationFailure> Errors,
        string? BadRequestDetail)
    {
        public static PayloadValidationResult Valid { get; } = new(true, Array.Empty<ValidationFailure>(), null);

        public static PayloadValidationResult BadRequest(string detail) => new(false, Array.Empty<ValidationFailure>(), detail);
    }

    internal static async Task<PayloadValidationResult> ValidateAsync(
        WebServiceMessage message,
        IServiceProvider services,
        CancellationToken cancellationToken) =>
        TryGetPayload(message, out var payload)
            ? await ValidatePayloadAsync(payload!, services, cancellationToken)
            : PayloadValidationResult.BadRequest("Invalid message payload or type.");

    private static bool TryGetPayload(WebServiceMessage message, out object? payload)
    {
        try
        {
            payload = message.GetBodyObject();
            return true;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or TypeLoadException
            or FileNotFoundException or ArgumentNullException or InvalidOperationException)
        {
            payload = null;
            return false;
        }
    }

    private static async Task<PayloadValidationResult> ValidatePayloadAsync(
        object payload,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        if (payload is not IRemotableRequest and not IRemotableEvent)
        {
            return PayloadValidationResult.BadRequest(
                $"Type {payload.GetType().FullName} is not a remotable request or event.");
        }

        var validatorType = typeof(IValidator<>).MakeGenericType(payload.GetType());
        if (services.GetService(validatorType) is not IValidator validator)
        {
            return PayloadValidationResult.Valid;
        }

        var result = await validator.ValidateAsync(new ValidationContext<object>(payload), cancellationToken);
        return result.IsValid
            ? PayloadValidationResult.Valid
            : new PayloadValidationResult(false, result.Errors, null);
    }
}
