using System.Reflection;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Messaging;
using FluentValidation;
using FluentValidation.Results;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Validates the request a <see cref="WebServiceMessage"/> carries with the FluentValidation validator registered for
/// its runtime type. A payload that cannot be read, or has no validator, is a bad request.
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
        var validatorInterface = typeof(IValidator<>).MakeGenericType(payload.GetType());
        if (services.GetService(validatorInterface) is not { } validator)
        {
            return PayloadValidationResult.BadRequest($"No validator registered for type {payload.GetType().FullName}.");
        }

        var validateMethod = validatorInterface.GetMethod("ValidateAsync", [payload.GetType(), typeof(CancellationToken)]);
        if (validateMethod is null)
        {
            return PayloadValidationResult.BadRequest("Validation configuration error.");
        }

        var validationResult = await InvokeAsync(validateMethod, validator, payload, cancellationToken);
        return validationResult.IsValid
            ? PayloadValidationResult.Valid
            : new PayloadValidationResult(false, validationResult.Errors, null);
    }

    /// <summary>Calls <c>IValidator&lt;T&gt;.ValidateAsync</c> for a payload whose type is known only at run time.</summary>
    private static async Task<ValidationResult> InvokeAsync(
        MethodInfo validateMethod,
        object validator,
        object payload,
        CancellationToken cancellationToken)
    {
        var validateTask = (Task)validateMethod.Invoke(validator, [payload, cancellationToken])!;
        await validateTask.ConfigureAwait(false);
        var resultProperty = validateTask.GetType().GetProperty(nameof(Task<>.Result))!;
        return (ValidationResult)resultProperty.GetValue(validateTask)!;
    }
}
