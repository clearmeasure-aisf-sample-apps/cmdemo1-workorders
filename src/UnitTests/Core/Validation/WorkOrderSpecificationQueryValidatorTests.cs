using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WorkOrderSpecificationQueryValidatorTests
{
    private readonly WorkOrderSpecificationQueryValidator _validator = new();

    [Test]
    public void ShouldPass_WhenNoFiltersSet()
    {
        _validator.TestValidate(new WorkOrderSpecificationQuery()).IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldPass_WhenAllFiltersWithinBounds()
    {
        var query = new WorkOrderSpecificationQuery
        {
            SearchText = new string('a', WorkOrderSpecificationQueryValidator.SearchTextMaxLength),
            Building = new string('b', WorkOrder.BuildingMaxLength),
            Room = new string('r', WorkOrder.RoomNumberMaxLength),
            CompletedWithinDays = WorkOrderSpecificationQueryValidator.CompletedWithinDaysMax,
            Priority = WorkOrderPriority.Low
        };

        _validator.TestValidate(query).IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenSearchTextTooLong()
    {
        var query = new WorkOrderSpecificationQuery
        {
            SearchText = new string('a', WorkOrderSpecificationQueryValidator.SearchTextMaxLength + 1)
        };

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.SearchText);
    }

    [Test]
    public void ShouldFail_WhenBuildingTooLong()
    {
        var query = new WorkOrderSpecificationQuery { Building = new string('b', WorkOrder.BuildingMaxLength + 1) };

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.Building);
    }

    [Test]
    public void ShouldFail_WhenRoomTooLong()
    {
        var query = new WorkOrderSpecificationQuery { Room = new string('r', WorkOrder.RoomNumberMaxLength + 1) };

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.Room);
    }

    [TestCase(0)]
    [TestCase(3651)]
    public void ShouldFail_WhenCompletedWithinDaysOutOfRange(int days)
    {
        var query = new WorkOrderSpecificationQuery { CompletedWithinDays = days };

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.CompletedWithinDays);
    }

    [Test]
    public void ShouldFail_WhenPriorityUndefined()
    {
        var query = new WorkOrderSpecificationQuery { Priority = (WorkOrderPriority)9 };

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.Priority);
    }
}
