using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.Core.Validation;

/// <summary>
/// DI-injected in-process shape rules for <see cref="CreateVendorCommand"/> (#1824). Wraps a
/// <see cref="CreateVendorCommandValidator"/> instance that is not registered as
/// <c>IValidator&lt;T&gt;</c> (Lamar would otherwise feed it to MediatR <c>ValidationBehavior</c>).
/// </summary>
public sealed class CreateVendorCommandShapeRules(CreateVendorCommandValidator validator)
{
    /// <summary>FluentValidation error messages, or an empty list when the command passes.</summary>
    public IReadOnlyList<string> ErrorMessages(CreateVendorCommand command)
    {
        var result = validator.Validate(command);
        return result.IsValid
            ? []
            : result.Errors.Select(failure => failure.ErrorMessage).ToList();
    }
}
