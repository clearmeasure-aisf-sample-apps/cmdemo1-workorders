using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelTitleTests
{
    [Test]
    public void ShouldRejectMissingTitle()
    {
        var model = ValidModel();
        model.Title = null;

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)));
    }

    [Test]
    public void ShouldRejectEmptyTitle()
    {
        var model = ValidModel();
        model.Title = string.Empty;

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)));
    }

    [Test]
    public void ShouldRejectWhitespaceOnlyTitle()
    {
        var model = ValidModel();
        model.Title = "   ";

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)));
    }

    [Test]
    public void ShouldAcceptTitleAtMaxLength()
    {
        var model = ValidModel();
        model.Title = new string('T', WorkOrder.TitleMaxLength);

        var results = Validate(model);

        results.ShouldNotContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)));
    }

    [Test]
    public void ShouldRejectTitleLongerThanMaxLength_WithExpectedMessage()
    {
        var model = ValidModel();
        model.Title = new string('T', WorkOrder.TitleMaxLength + 1);

        var results = Validate(model);

        results.Single(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)))
            .ErrorMessage.ShouldBe("Title cannot exceed 300 characters.");
    }

    [Test]
    public void ShouldAcceptTitleWithSurroundingSpaces()
    {
        var model = ValidModel();
        model.Title = "  Title with spaces  ";

        var results = Validate(model);

        results.ShouldNotContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)));
    }

    private static WorkOrderManageModel ValidModel()
    {
        return new WorkOrderManageModel
        {
            Title = "Title",
            Description = "Description"
        };
    }

    private static List<ValidationResult> Validate(WorkOrderManageModel model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, true);
        return results;
    }
}
