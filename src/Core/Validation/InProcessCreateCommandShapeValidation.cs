using FluentValidation;
using FluentValidation.Results;

namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// #1824: runs the HTTP-envelope FluentValidation type for a deferred create command without
/// registering it as MediatR <c>IValidator&lt;T&gt;</c>. Handlers map the error messages onto
/// Failed create results. Uses <see cref="Activator.CreateInstance(Type)"/> so production code
/// does not <c>new</c> an <see cref="AbstractValidator{T}"/> (architecture gate).
/// </summary>
public static class InProcessCreateCommandShapeValidation
{
    /// <summary>
    /// Error messages from the deferred FluentValidation type for <typeparamref name="TCommand"/>,
    /// or an empty list when the catalog has no entry or the command passes.
    /// </summary>
    public static IReadOnlyList<string> ErrorMessages<TCommand>(TCommand command)
        where TCommand : class
    {
        var validatorType = ValidatorTypeFor(typeof(TCommand));
        if (validatorType is null)
        {
            return [];
        }

        var validator = (IValidator)Activator.CreateInstance(validatorType)!;
        var result = validator.Validate(new ValidationContext<object>(command));
        return ErrorMessagesOf(result);
    }

    private static Type? ValidatorTypeFor(Type requestType)
    {
        foreach (var (catalogRequest, validatorType) in HttpEnvelopeValidationDeferral.Entries)
        {
            if (catalogRequest == requestType)
            {
                return validatorType;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> ErrorMessagesOf(ValidationResult result)
    {
        if (result.IsValid)
        {
            return [];
        }

        return result.Errors.Select(failure => failure.ErrorMessage).ToList();
    }
}
