using ClearMeasure.Bootcamp.Core;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// The one place FluentValidation runs for MediatR requests: resolves every
/// <see cref="IValidator{TRequest}"/> from DI and throws <see cref="ValidationException"/>
/// before the handler when any rule fails. Requests marked
/// <see cref="IHttpEnvelopeValidation"/> are skipped here so HTTP still validates them
/// while in-process callers keep today's handler results (#1804).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IHttpEnvelopeValidation)
        {
            return await next();
        }

        var failures = await CollectFailuresAsync(request, cancellationToken);
        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }

    private async Task<List<ValidationFailure>> CollectFailuresAsync(
        TRequest request,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();
        var context = new ValidationContext<TRequest>(request);
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors);
        }

        return failures;
    }
}
