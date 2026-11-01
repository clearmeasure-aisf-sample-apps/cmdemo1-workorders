using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelRoomNumberTests
{
    [Test]
    public void ShouldAllowMissingRoomNumber()
    {
        var model = ValidModel();
        model.RoomNumber = null;

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldAcceptRoomNumberAtEntryMaxLength()
    {
        var model = ValidModel();
        model.RoomNumber = new string('R', WorkOrder.RoomNumberEntryMaxLength);
        model.OriginalRoomNumber = model.RoomNumber;

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldRejectRoomNumberLongerThanEntryMaxLength()
    {
        var model = ValidModel();
        model.RoomNumber = new string('R', WorkOrder.RoomNumberEntryMaxLength + 1);
        model.OriginalRoomNumber = null;

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.RoomNumber)));
    }

    [Test]
    public void ShouldAllowUnchangedLongRoomNumber()
    {
        var longRoom = new string('R', WorkOrder.RoomNumberMaxLength);
        var model = ValidModel();
        model.RoomNumber = longRoom;
        model.OriginalRoomNumber = longRoom;

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void RoomNumberMaxLength_ShouldBe900()
    {
        WorkOrder.RoomNumberMaxLength.ShouldBe(900);
    }

    [Test]
    public void RoomNumberEntryMaxLength_ShouldBe50()
    {
        WorkOrder.RoomNumberEntryMaxLength.ShouldBe(50);
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
