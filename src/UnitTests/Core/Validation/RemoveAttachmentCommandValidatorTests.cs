using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class RemoveAttachmentCommandValidatorTests
{
    private readonly RemoveAttachmentCommandValidator _validator = new();

    [Test]
    public void ShouldPass_WhenAllFieldsValid()
    {
        _validator.TestValidate(new RemoveAttachmentCommand(Guid.NewGuid(), Guid.NewGuid(), "hsimpson"))
            .IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenWorkOrderIdIsEmpty()
    {
        _validator.TestValidate(new RemoveAttachmentCommand(Guid.Empty, Guid.NewGuid(), "hsimpson"))
            .ShouldHaveValidationErrorFor(x => x.WorkOrderId);
    }

    [Test]
    public void ShouldFail_WhenAttachmentIdIsEmpty()
    {
        _validator.TestValidate(new RemoveAttachmentCommand(Guid.NewGuid(), Guid.Empty, "hsimpson"))
            .ShouldHaveValidationErrorFor(x => x.AttachmentId);
    }

    [Test]
    public void ShouldFail_WhenUserNameIsEmpty()
    {
        _validator.TestValidate(new RemoveAttachmentCommand(Guid.NewGuid(), Guid.NewGuid(), ""))
            .ShouldHaveValidationErrorFor(x => x.UserName);
    }
}
