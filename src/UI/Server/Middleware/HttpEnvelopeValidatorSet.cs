using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// DI-owned FluentValidation instances for <see cref="HttpEnvelopeValidationDeferral"/> (#1804 c /
/// #1824). Built once from the Core deferral list (each entry carries its own factory) so the types are
/// never registered as unkeyed <see cref="IValidator{T}"/> (DI would otherwise feed them to
/// the mediator <c>ValidationBehavior</c>).
/// </summary>
internal sealed class HttpEnvelopeValidatorSet
{
    private readonly IReadOnlyDictionary<Type, IValidator> _byRequest;

    public HttpEnvelopeValidatorSet()
    {
        _byRequest = HttpEnvelopeValidationDeferral.Entries.ToDictionary(entry => entry.RequestType, entry => entry.Create());
    }

    /// <summary>Deferred validator for the payload's request type, or null when not deferred.</summary>
    public IValidator? For(object payload) =>
        _byRequest.TryGetValue(payload.GetType(), out var validator) ? validator : null;
}
