using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using FluentValidation;

// ReSharper disable UnusedType.Global -- discovered via FluentValidation's assembly scan.
namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class SaveSearchViewCommandValidator : AbstractValidator<SaveSearchViewCommand>
{
    public SaveSearchViewCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(SavedSearchView.MaxNameLength);
        RuleFor(x => x.Query).NotNull().MaximumLength(SavedSearchView.MaxQueryLength);
    }
}
