using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelBuildingTests
{
    [Test]
    public void ShouldAllowMissingBuilding()
    {
        var model = ValidModel();
        model.Building = null;

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldAllowEmptyBuilding()
    {
        var model = ValidModel();
        model.Building = string.Empty;

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldAcceptBuildingAtMaxLength()
    {
        var model = ValidModel();
        model.Building = new string('B', WorkOrder.BuildingMaxLength);

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldRejectBuildingLongerThanMaxLength_WithExpectedMessage()
    {
        var model = ValidModel();
        model.Building = new string('B', WorkOrder.BuildingMaxLength + 1);

        var results = Validate(model);

        results.Single(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Building)))
            .ErrorMessage.ShouldBe("Building cannot exceed 100 characters.");
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
