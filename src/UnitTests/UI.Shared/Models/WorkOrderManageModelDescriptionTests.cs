using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelDescriptionTests
{
    [Test]
    public void ShouldRejectMissingDescription()
    {
        var model = ValidModel();
        model.Description = null;

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Description)));
    }

    [Test]
    public void ShouldRejectEmptyDescription()
    {
        var model = ValidModel();
        model.Description = string.Empty;

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Description)));
    }

    [Test]
    public void ShouldAcceptDescriptionAtMaxLength()
    {
        var model = ValidModel();
        model.Description = new string('x', WorkOrder.DescriptionMaxLength);

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldRejectDescriptionLongerThanMaxLength()
    {
        var model = ValidModel();
        model.Description = new string('x', WorkOrder.DescriptionMaxLength + 1);

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Description)));
    }

    [Test]
    public void ShouldRejectDescriptionLongerThanMaxLength_WithExpectedMessage()
    {
        var model = ValidModel();
        model.Description = new string('x', WorkOrder.DescriptionMaxLength + 1);

        var results = Validate(model);

        results.Single(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Description)))
            .ErrorMessage.ShouldBe("Description cannot exceed 4000 characters.");
    }

    [Test]
    public void DescriptionMaxLength_ShouldBe4000()
    {
        WorkOrder.DescriptionMaxLength.ShouldBe(4000);
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
