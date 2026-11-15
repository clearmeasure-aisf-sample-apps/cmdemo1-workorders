using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// HTTP-envelope hand-off for validators listed in
/// <see cref="HttpEnvelopeValidationDeferral"/> (#1804 decision c / #1824). Resolves
/// <see cref="HttpEnvelopeValidatorSet"/> from DI so <c>ValidationBehavior</c> never sees those
/// rules in-process.
/// </summary>
internal static class HttpEnvelopeFluentValidators
{
    /// <summary>True when assembly scanning must skip this validator for MediatR <see cref="IValidator{T}"/>.</summary>
    internal static bool IsDeferredFromMediatR(Type validatorType) =>
        HttpEnvelopeValidationDeferral.IsDeferredValidator(validatorType);

    /// <summary>
    /// Resolves the deferred FluentValidation validator for a payload from DI, when the
    /// request type is on the #1824 deferral list.
    /// </summary>
    internal static IValidator? Resolve(object payload, IServiceProvider services) =>
        services.GetService(typeof(HttpEnvelopeValidatorSet)) is HttpEnvelopeValidatorSet set
            ? set.For(payload)
            : null;
}
