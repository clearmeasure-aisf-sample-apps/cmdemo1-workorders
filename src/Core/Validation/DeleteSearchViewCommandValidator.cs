using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using FluentValidation;

// ReSharper disable UnusedType.Global -- discovered via FluentValidation's assembly scan.
namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class DeleteSearchViewCommandValidator : AbstractValidator<DeleteSearchViewCommand>
{
    public DeleteSearchViewCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}
