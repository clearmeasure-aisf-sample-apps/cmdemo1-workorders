using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;

// ReSharper disable UnusedType.Global -- discovered via FluentValidation's assembly scan.
namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class SavedSearchViewsQueryValidator : AbstractValidator<SavedSearchViewsQuery>
{
    public SavedSearchViewsQueryValidator()
    {
        RuleFor(x => x.UserName).NotEmpty();
    }
}
