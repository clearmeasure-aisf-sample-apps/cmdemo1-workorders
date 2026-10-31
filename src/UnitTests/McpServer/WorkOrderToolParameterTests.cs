using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class WorkOrderToolParameterTests
{
    [Test]
    public void ShouldParseNullPriorityAsDefaultMedium()
    {
        var (result, error) = WorkOrderTools.ParseAndValidatePriority(null);

        result.ShouldBe(WorkOrderPriority.Medium);
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldParseEmptyStringPriorityAsDefaultMedium()
    {
        var (result, error) = WorkOrderTools.ParseAndValidatePriority(string.Empty);

        result.ShouldBe(WorkOrderPriority.Medium);
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldParseWhitespacePriorityAsDefaultMedium()
    {
        var (result, error) = WorkOrderTools.ParseAndValidatePriority("   ");

        result.ShouldBe(WorkOrderPriority.Medium);
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldParseLowPriority()
    {
        var (result, error) = WorkOrderTools.ParseAndValidatePriority("Low");

        result.ShouldBe(WorkOrderPriority.Low);
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldParseMediumPriority()
    {
        var (result, error) = WorkOrderTools.ParseAndValidatePriority("Medium");

        result.ShouldBe(WorkOrderPriority.Medium);
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldParseHighPriority()
    {
        var (result, error) = WorkOrderTools.ParseAndValidatePriority("High");

        result.ShouldBe(WorkOrderPriority.High);
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldParseLowercasePriority()
    {
        var (result, error) = WorkOrderTools.ParseAndValidatePriority("low");

        result.ShouldBe(WorkOrderPriority.Low);
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldParseUppercasePriority()
    {
        var (result, error) = WorkOrderTools.ParseAndValidatePriority("HIGH");

        result.ShouldBe(WorkOrderPriority.High);
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldParsePriorityWithWhitespace()
    {
        var (result, error) = WorkOrderTools.ParseAndValidatePriority("  medium  ");

        result.ShouldBe(WorkOrderPriority.Medium);
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldRejectInvalidPriorityName()
    {
        var (_, error) = WorkOrderTools.ParseAndValidatePriority("Urgent");

        error!.ShouldContain("Invalid priority");
        error!.ShouldContain("Low");
        error!.ShouldContain("Medium");
        error!.ShouldContain("High");
    }

    [Test]
    public void ShouldRejectNumericPriorityString()
    {
        var (_, error) = WorkOrderTools.ParseAndValidatePriority("2");

        error!.ShouldContain("Invalid priority");
    }

    [Test]
    public void ShouldValidateNullBuildingAsNull()
    {
        var (result, error) = WorkOrderTools.ValidateBuilding(null);

        result.ShouldBeNull();
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldValidateEmptyStringBuildingAsNull()
    {
        var (result, error) = WorkOrderTools.ValidateBuilding(string.Empty);

        result.ShouldBeNull();
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldValidateWhitespaceBuildingAsNull()
    {
        var (result, error) = WorkOrderTools.ValidateBuilding("   ");

        result.ShouldBeNull();
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldValidateBuildingWithContent()
    {
        var (result, error) = WorkOrderTools.ValidateBuilding("Main Hall");

        result.ShouldBe("Main Hall");
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldTrimBuildingWhitespace()
    {
        var (result, error) = WorkOrderTools.ValidateBuilding("  East Wing  ");

        result.ShouldBe("East Wing");
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldValidateBuildingAtMaxLength()
    {
        var exactLength = new string('x', WorkOrder.BuildingMaxLength);
        var (result, error) = WorkOrderTools.ValidateBuilding(exactLength);

        result.ShouldBe(exactLength);
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldRejectBuildingExceedingMaxLength()
    {
        var tooLong = new string('x', WorkOrder.BuildingMaxLength + 1);
        var (_, error) = WorkOrderTools.ValidateBuilding(tooLong);

        error!.ShouldContain("Building cannot exceed");
        error!.ShouldContain("100");
    }

    [Test]
    public void ShouldValidateBuildingBoundaryAt100Characters()
    {
        var exactly100 = new string('a', 100);
        var (result, error) = WorkOrderTools.ValidateBuilding(exactly100);

        result.ShouldBe(exactly100);
        error.ShouldBeNull();
    }

    [Test]
    public void ShouldRejectBuildingAt101Characters()
    {
        var exactly101 = new string('a', 101);
        var (_, error) = WorkOrderTools.ValidateBuilding(exactly101);

        error.ShouldNotBeNull();
    }

    [Test]
    public void ShouldReturnPermissionMessageForDraftUpdateNotAllowed()
    {
        var workOrder = new WorkOrder
        {
            Number = "WO-1",
            Status = WorkOrderStatus.Draft,
            Creator = new Employee("jsmith", "Jane", "Smith", "jane@test.com")
        };

        WorkOrderTools.FormatUpdateNotAllowed(workOrder)
            .ShouldBe("Only the creator 'jsmith' can update draft work order 'WO-1'.");
    }

    [Test]
    public void ShouldReturnPermissionMessageWithoutCreatorNameWhenCreatorMissing()
    {
        var workOrder = new WorkOrder { Number = "WO-2", Status = WorkOrderStatus.Draft, Creator = null };

        WorkOrderTools.FormatUpdateNotAllowed(workOrder)
            .ShouldBe("Only the creator can update draft work order 'WO-2'.");
    }

    [Test]
    public void ShouldReturnInvalidStatusMessageForNonDraftUpdateNotAllowed()
    {
        var workOrder = new WorkOrder
        {
            Number = "WO-3",
            Status = WorkOrderStatus.Assigned,
            Creator = new Employee("jsmith", "Jane", "Smith", "jane@test.com")
        };

        var message = WorkOrderTools.FormatUpdateNotAllowed(workOrder);

        message.ShouldContain("cannot be executed");
        message.ShouldContain("'Assigned' status");
        message.ShouldNotContain("Only the creator");
    }
}
