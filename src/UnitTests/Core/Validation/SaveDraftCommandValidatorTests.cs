using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class SaveDraftCommandValidatorTests
{
    private SaveDraftCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new SaveDraftCommandValidator();
    }

    [Test]
    public void ShouldPass_WhenTitleIsAtMaxLength()
    {
        var command = CreateCommand(new string('T', WorkOrder.TitleMaxLength));

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenTitleExceedsMaxLength()
    {
        var command = CreateCommand(new string('T', WorkOrder.TitleMaxLength + 1));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.WorkOrder.Title)
            .WithErrorMessage("Title cannot exceed 300 characters.");
    }

    [Test]
    public void ShouldPass_WhenTitleIsNull()
    {
        var command = CreateCommand(null);

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    private static SaveDraftCommand CreateCommand(string? title)
    {
        var creator = new Employee("creator1", "Creator", "One", "creator1@example.com");
        var workOrder = new WorkOrder { Title = title, Creator = creator };
        return new SaveDraftCommand(workOrder, creator);
    }
}
