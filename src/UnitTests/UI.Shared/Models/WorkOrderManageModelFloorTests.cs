using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelFloorTests
{
    [Test]
    public void ShouldAllowMissingFloor()
    {
        var model = ValidModel();
        model.Floor = null;

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldAllowEmptyFloor()
    {
        var model = ValidModel();
        model.Floor = string.Empty;

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldAcceptFloorAtMaxLength()
    {
        var model = ValidModel();
        model.Floor = new string('F', WorkOrder.FloorMaxLength);

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldRejectFloorLongerThanMaxLength_WithExpectedMessage()
    {
        var model = ValidModel();
        model.Floor = new string('F', WorkOrder.FloorMaxLength + 1);

        var results = Validate(model);

        results.Single(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Floor)))
            .ErrorMessage.ShouldBe("Floor cannot exceed 20 characters.");
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
