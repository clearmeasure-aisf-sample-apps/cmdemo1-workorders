using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// DI-owned FluentValidation instances for <see cref="HttpEnvelopeValidationDeferral"/> (#1804 c /
/// #1824). Built once from the Core catalog via <see cref="ActivatorUtilities"/> so the types are
/// never registered as unkeyed <see cref="IValidator{T}"/> (Lamar would otherwise feed them to
/// MediatR <c>ValidationBehavior</c>).
/// </summary>
internal sealed class HttpEnvelopeValidatorSet
{
    private readonly IReadOnlyDictionary<Type, IValidator> _byRequest;

    public HttpEnvelopeValidatorSet(IServiceProvider services)
    {
        var map = new Dictionary<Type, IValidator>();
        foreach (var (requestType, validatorType) in HttpEnvelopeValidationDeferral.Entries)
        {
            map[requestType] = (IValidator)ActivatorUtilities.CreateInstance(services, validatorType);
        }

        _byRequest = map;
    }

    /// <summary>Deferred validator for the payload's request type, or null when not deferred.</summary>
    public IValidator? For(object payload) =>
        _byRequest.TryGetValue(payload.GetType(), out var validator) ? validator : null;
}
