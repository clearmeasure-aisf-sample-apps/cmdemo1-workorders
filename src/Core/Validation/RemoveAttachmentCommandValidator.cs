using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using FluentValidation;

// ReSharper disable UnusedType.Global -- discovered via FluentValidation's assembly scan.
namespace ClearMeasure.Bootcamp.Core.Validation;

public sealed class RemoveAttachmentCommandValidator : AbstractValidator<RemoveAttachmentCommand>
{
    public RemoveAttachmentCommandValidator()
    {
        RuleFor(x => x.WorkOrderId).NotEmpty();
        RuleFor(x => x.AttachmentId).NotEmpty();
        RuleFor(x => x.UserName).NotEmpty();
    }
}
