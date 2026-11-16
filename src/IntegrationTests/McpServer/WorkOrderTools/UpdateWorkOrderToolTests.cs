using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests;
using Shouldly;
using ProductionWorkOrderTools = ClearMeasure.Bootcamp.McpServer.Tools.WorkOrderTools;
using TestWorkOrderBuilder = ClearMeasure.Bootcamp.UnitTests.TestSupport.WorkOrderBuilder;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer.WorkOrderTools;

[TestFixture]
public class UpdateWorkOrderToolTests
{
    [Test]
    public async Task ShouldUpdatePriority()
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Jane", "Smith");
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            creator,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Update priority")
                .CreatedBy(creator)
                .WithPriority(WorkOrderPriority.Low)
                .Build());

        var result = await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            priority: "High");

        McpWorkOrderSteps.ReadString(result, "Priority").ShouldBe("High");
        (await McpWorkOrderSteps.LoadByNumberAsync(number))!.Priority.ShouldBe(WorkOrderPriority.High);
    }

    [Test]
    public async Task ShouldUpdateBuildingAndRoom()
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Jane", "Smith");
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            creator,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Update building")
                .CreatedBy(creator)
                .InBuilding("Old Building")
                .InRoom("101")
                .Build());

        var result = await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            building: "New Building",
            roomNumber: "202");

        McpWorkOrderSteps.ReadString(result, "Building").ShouldBe("New Building");
        McpWorkOrderSteps.ReadString(result, "RoomNumber").ShouldBe("202");
        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number);
        reloaded!.Building.ShouldBe("New Building");
        reloaded.RoomNumber.ShouldBe("202");
    }

    [Test]
    public async Task ShouldUpdateDueDate()
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Jane", "Smith");
        var number = TestData.Number("W");
        var newDueDate = new DateOnly(2026, 12, 25);
        await McpWorkOrderSteps.PersistAsync(
            creator,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Update due date")
                .CreatedBy(creator)
                .DueOn(new DateOnly(2026, 8, 1))
                .Build());

        var result = await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            dueDate: newDueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        McpWorkOrderSteps.ReadString(result, "DueDate").ShouldBe("2026-12-25");
        (await McpWorkOrderSteps.LoadByNumberAsync(number))!.DueDate.ShouldBe(newDueDate);
    }

    [Test]
    public async Task ShouldLeaveOmittedFieldsUnchanged()
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Jane", "Smith");
        var number = TestData.Number("W");
        var originalDueDate = new DateOnly(2026, 9, 1);
        await McpWorkOrderSteps.PersistAsync(
            creator,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Omitted fields")
                .CreatedBy(creator)
                .WithPriority(WorkOrderPriority.High)
                .InBuilding("West Wing")
                .InRoom("300")
                .DueOn(originalDueDate)
                .Build());

        await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            roomNumber: "301");

        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number);
        reloaded!.Priority.ShouldBe(WorkOrderPriority.High);
        reloaded.Building.ShouldBe("West Wing");
        reloaded.RoomNumber.ShouldBe("301");
        reloaded.DueDate.ShouldBe(originalDueDate);
    }

    [Test]
    public async Task ShouldClearEmptyBuilding()
    {
        var (creator, number) = await McpWorkOrderSteps.PersistDraftAsync(new DraftArrange { Title = "Clear building", Building = "Main Hall" });

        await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            building: string.Empty);

        (await McpWorkOrderSteps.LoadByNumberAsync(number))!.Building.ShouldBeNull();
    }

    [Test]
    public async Task ShouldClearEmptyRoom()
    {
        var (creator, number) = await McpWorkOrderSteps.PersistDraftAsync(new DraftArrange { Title = "Clear room", Room = "501" });

        await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            roomNumber: "   ");

        (await McpWorkOrderSteps.LoadByNumberAsync(number))!.RoomNumber.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldClearEmptyDueDate()
    {
        var (creator, number) = await McpWorkOrderSteps.PersistDraftAsync(new DraftArrange { Title = "Clear due date", DueDate = new DateOnly(2026, 10, 1) });

        await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            dueDate: string.Empty);

        (await McpWorkOrderSteps.LoadByNumberAsync(number))!.DueDate.ShouldBeNull();
    }

    [Test]
    public async Task ShouldReturnErrorForInvalidPriority()
    {
        var (creator, number) = await McpWorkOrderSteps.PersistDraftAsync(new DraftArrange { Title = "Invalid priority test", Priority = WorkOrderPriority.Medium });

        var result = await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            priority: "Urgent");

        result.ShouldContain("Invalid priority");
        (await McpWorkOrderSteps.LoadByNumberAsync(number))!.Priority.ShouldBe(WorkOrderPriority.Medium);
    }

    [Test]
    public async Task ShouldReturnErrorForBuildingExceedingMaxLength()
    {
        var (creator, number) = await McpWorkOrderSteps.PersistDraftAsync(new DraftArrange { Title = "Building too long test", Building = "Original" });

        var result = await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            building: new string('x', WorkOrder.BuildingMaxLength + 1));

        result.ShouldContain("Building cannot exceed");
        (await McpWorkOrderSteps.LoadByNumberAsync(number))!.Building.ShouldBe("Original");
    }

    [Test]
    public async Task ShouldReturnErrorForMalformedDueDate()
    {
        var (creator, number) = await McpWorkOrderSteps.PersistDraftAsync(new DraftArrange { Title = "Bad date test", DueDate = new DateOnly(2026, 8, 1) });

        var result = await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            dueDate: "not-a-date");

        result.ShouldContain("Invalid due date");
        (await McpWorkOrderSteps.LoadByNumberAsync(number))!.DueDate.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Test]
    public async Task ShouldReturnErrorForUnknownWorkOrder()
    {
        var creatorUserName = TestData.UserName("creator");
        await McpWorkOrderSteps.SeedCreatorAsync(creatorUserName);

        var result = await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            "UNKNOWN-WO",
            creatorUserName,
            priority: "Low");

        result.ShouldBe("No work order found with number 'UNKNOWN-WO'.");
    }

    [Test]
    public async Task ShouldReturnErrorForUnknownUser()
    {
        var (_, number) = await McpWorkOrderSteps.PersistDraftAsync(new DraftArrange { Title = "Unknown user test" });

        var result = await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            "unknown-user",
            priority: "Low");

        result.ShouldBe("Employee with username 'unknown-user' not found.");
    }

    [Test]
    public async Task ShouldReturnErrorForNonDraftWorkOrder()
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Jane", "Smith");
        var assignee = ObjectMother.Employee(TestData.UserName("worker"), "Sam", "Worker");
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            creator,
            assignee,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Assigned work order")
                .CreatedBy(creator)
                .AssignedTo(assignee)
                .WithStatus(WorkOrderStatus.Assigned)
                .Build());

        var result = await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            priority: "High");

        result.ShouldContain("cannot be executed");
        result.ShouldContain("Assigned");
    }

    [Test]
    public async Task ShouldReturnErrorForNonCreator()
    {
        var (creator, otherUser, number) = await McpWorkOrderSteps.PersistDraftWithOtherUserAsync(
            new DraftArrange
            {
                Title = "Draft work order",
                Priority = WorkOrderPriority.Low,
                Building = "Original",
                Room = "101",
                DueDate = new DateOnly(2026, 6, 15)
            });

        var result = await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            otherUser.UserName,
            priority: "High",
            building: "Changed",
            roomNumber: "999",
            dueDate: "2026-12-25");

        result.ShouldBe($"Only the creator '{creator.UserName}' can update draft work order '{number}'.");
    }

    [Test]
    public async Task ShouldLeaveFieldsUnchangedWhenNonCreatorUpdateFails()
    {
        var originalDueDate = new DateOnly(2026, 6, 15);
        var (_, otherUser, number) = await McpWorkOrderSteps.PersistDraftWithOtherUserAsync(
            new DraftArrange
            {
                Title = "Draft work order",
                Priority = WorkOrderPriority.Low,
                Building = "Original",
                Room = "101",
                DueDate = originalDueDate
            });

        await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            otherUser.UserName,
            priority: "High",
            building: "Changed",
            roomNumber: "999",
            dueDate: "2026-12-25");

        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number);
        reloaded!.Priority.ShouldBe(WorkOrderPriority.Low);
        reloaded.Building.ShouldBe("Original");
        reloaded.RoomNumber.ShouldBe("101");
        reloaded.DueDate.ShouldBe(originalDueDate);
        reloaded.Status.ShouldBe(WorkOrderStatus.Draft);
    }

    [Test]
    public async Task ShouldNotChangeCreatedOrCompletedDates()
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Jane", "Smith");
        var number = TestData.Number("W");
        var createdDate = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var completedDate = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        await McpWorkOrderSteps.PersistAsync(
            creator,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Dates test")
                .CreatedBy(creator)
                .CreatedAt(createdDate)
                .CompletedAt(completedDate)
                .Build());

        await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            priority: "High");

        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number);
        reloaded!.CreatedDate.ShouldBe(createdDate);
        reloaded.CompletedDate.ShouldBe(completedDate);
    }

    [Test]
    public async Task ShouldReturnErrorForBlankPriority()
    {
        var (creator, number) = await McpWorkOrderSteps.PersistDraftAsync(new DraftArrange { Title = "Blank priority test", Priority = WorkOrderPriority.High });

        var result = await ProductionWorkOrderTools.UpdateWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            priority: "   ");

        result.ShouldContain("Priority cannot be cleared");
        (await McpWorkOrderSteps.LoadByNumberAsync(number))!.Priority.ShouldBe(WorkOrderPriority.High);
    }

}
