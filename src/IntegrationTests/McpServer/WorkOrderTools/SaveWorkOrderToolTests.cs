using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UnitTests;
using Shouldly;
using ProductionWorkOrderTools = ClearMeasure.Bootcamp.McpServer.Tools.WorkOrderTools;
using TestWorkOrderBuilder = ClearMeasure.Bootcamp.UnitTests.TestSupport.WorkOrderBuilder;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer.WorkOrderTools;

[TestFixture]
public class SaveWorkOrderToolTests
{
    [Test]
    public async Task ShouldChangeTitleAndClearDueDateWhilePreservingOtherFields()
    {
        const string instructions = "Check preschool gate latch before mowing";
        const string room = "Front lawn";
        var (creatorUserName, number) = await McpWorkOrderSteps.CreateDraftViaToolAsync(
            new McpDraftCreate
            {
                Title = "mow front grass",
                Description = "Weekly mowing",
                RoomNumber = room,
                DueDate = "2027-06-15",
                Instructions = instructions
            });

        var saveResult = await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creatorUserName,
            title: "Saturday mow",
            dueDate: string.Empty);

        saveResult.ShouldContain("Saturday mow");
        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number);
        reloaded!.Title.ShouldBe("Saturday mow");
        reloaded.DueDate.ShouldBeNull();
        reloaded.Instructions.ShouldBe(instructions);
        reloaded.RoomNumber.ShouldBe(room);
    }

    [Test]
    public async Task ShouldKeepCreatorStatusAndUnassignedWhenSavingDraft()
    {
        var (creatorUserName, number) = await McpWorkOrderSteps.CreateDraftViaToolAsync(
            new McpDraftCreate
            {
                Title = "mow front grass",
                Description = "Weekly mowing",
                RoomNumber = "Front lawn",
                DueDate = "2027-06-15",
                Instructions = "Check preschool gate latch before mowing"
            });

        await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creatorUserName,
            title: "Saturday mow",
            dueDate: string.Empty);

        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number);
        reloaded!.Status.ShouldBe(WorkOrderStatus.Draft);
        reloaded.Number.ShouldBe(number);
        reloaded.Creator!.UserName.ShouldBe(creatorUserName);
        reloaded.Assignee.ShouldBeNull();
    }

    [Test]
    public async Task ShouldLeaveOmittedFieldsUnchangedWhenOnlyRoomNumberProvided()
    {
        var (creatorUserName, number) = await McpWorkOrderSteps.CreateDraftViaToolAsync(new McpDraftCreate
        {
            Title = "Original title",
            Description = "Original description",
            RoomNumber = "Room A",
            DueDate = "2027-06-15",
            Instructions = "Original instructions"
        });

        await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creatorUserName,
            roomNumber: "Room B");

        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number);
        reloaded!.Title.ShouldBe("Original title");
        reloaded.Description.ShouldBe("Original description");
        reloaded.Instructions.ShouldBe("Original instructions");
        reloaded.DueDate.ShouldBe(new DateOnly(2027, 6, 15));
        reloaded.RoomNumber.ShouldBe("Room B");
    }

    [Test]
    public async Task ShouldPersistEmptyDescriptionInstructionsAndRoom()
    {
        var (creatorUserName, number) = await McpWorkOrderSteps.CreateDraftViaToolAsync(new McpDraftCreate
        {
            Title = "Has content",
            Description = "Has description",
            RoomNumber = "Room 101",
            Instructions = "Has instructions"
        });

        await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creatorUserName,
            description: string.Empty,
            instructions: "   ",
            roomNumber: string.Empty);

        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number);
        reloaded!.Description.ShouldBe(string.Empty);
        reloaded.Instructions.ShouldBe(string.Empty);
        reloaded.RoomNumber.ShouldBe(string.Empty);
        reloaded.Title.ShouldBe("Has content");
    }

    [Test]
    public async Task ShouldPersistValidDueDate()
    {
        var (creatorUserName, number) = await McpWorkOrderSteps.CreateDraftViaToolAsync(new McpDraftCreate { Title = "Due date save", Description = "Description" });
        var expectedDueDate = new DateOnly(2026, 11, 15);

        await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creatorUserName,
            dueDate: expectedDueDate.ToString("yyyy-MM-dd"));

        (await McpWorkOrderSteps.LoadByNumberAsync(number))!.DueDate.ShouldBe(expectedDueDate);
    }

    [Test]
    public async Task ShouldRejectInvalidDueDateWithoutChangingStoredDueDate()
    {
        var originalDueDate = new DateOnly(2027, 6, 15);
        var (creatorUserName, number) = await McpWorkOrderSteps.CreateDraftViaToolAsync(new McpDraftCreate
        {
            Title = "Invalid due date",
            Description = "Description",
            DueDate = originalDueDate.ToString("yyyy-MM-dd")
        });

        var result = await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creatorUserName,
            dueDate: "not-a-date");

        result.ShouldBe("Invalid due date 'not-a-date'. Use yyyy-MM-dd.");
        (await McpWorkOrderSteps.LoadByNumberAsync(number))!.DueDate.ShouldBe(originalDueDate);
    }

    [Test]
    public async Task ShouldRejectEmptyTitleWithoutChangingStoredRow()
    {
        var originalDueDate = new DateOnly(2027, 6, 15);
        var (creatorUserName, number) = await McpWorkOrderSteps.CreateDraftViaToolAsync(new McpDraftCreate
        {
            Title = "Keep this title",
            Description = "Description",
            DueDate = originalDueDate.ToString("yyyy-MM-dd")
        });

        var result = await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creatorUserName,
            title: string.Empty);

        result.ShouldBe("The Title field is required.");
        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number);
        reloaded!.Title.ShouldBe("Keep this title");
        reloaded.DueDate.ShouldBe(originalDueDate);
    }

    [Test]
    public async Task ShouldRejectWhitespaceTitleAndNotPersistDueDateInSameCall()
    {
        var originalDueDate = new DateOnly(2027, 6, 15);
        var (creatorUserName, number) = await McpWorkOrderSteps.CreateDraftViaToolAsync(new McpDraftCreate
        {
            Title = "Keep this title",
            Description = "Description",
            DueDate = originalDueDate.ToString("yyyy-MM-dd")
        });

        var result = await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creatorUserName,
            title: "   ",
            dueDate: "2026-12-25");

        result.ShouldBe("The Title field is required.");
        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number);
        reloaded!.Title.ShouldBe("Keep this title");
        reloaded.DueDate.ShouldBe(originalDueDate);
    }

    [Test]
    public async Task ShouldTruncateInstructionsTo4000Characters()
    {
        var (creatorUserName, number) = await McpWorkOrderSteps.CreateDraftViaToolAsync(new McpDraftCreate { Title = "Truncate instructions", Description = "Description" });
        var longInstructions = new string('x', WorkOrder.InstructionsMaxLength + 1);
        var expected = new string('x', WorkOrder.InstructionsMaxLength);

        var saveResult = await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creatorUserName,
            instructions: longInstructions);

        saveResult.ShouldContain("Truncate instructions");
        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number);
        reloaded!.Instructions!.Length.ShouldBe(WorkOrder.InstructionsMaxLength);
        reloaded.Instructions.ShouldBe(expected);
    }

    [Test]
    public async Task ShouldRejectRoomNumberLongerThan50CharactersOnNewWorkOrder()
    {
        var (creatorUserName, number) = await McpWorkOrderSteps.CreateDraftViaToolAsync(new McpDraftCreate { Title = "Reject long room", Description = "Description" });
        var tooLongRoom = new string('r', WorkOrder.RoomNumberEntryMaxLength + 1);

        var result = await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creatorUserName,
            roomNumber: tooLongRoom);

        result.ShouldContain("cannot exceed");
        (await McpWorkOrderSteps.LoadByNumberAsync(number))!.RoomNumber.ShouldBeNull();
    }

    [Test]
    public async Task ShouldAllowPreservingLongRoomNumberViaMcpOnExistingWorkOrder()
    {
        var (creatorUserName, number) = await McpWorkOrderSteps.CreateDraftViaToolAsync(new McpDraftCreate { Title = "Preserve long room", Description = "Description" });
        var longRoom = new string('r', WorkOrder.RoomNumberMaxLength);
        var legacy = (await McpWorkOrderSteps.LoadByNumberAsync(number))!;
        legacy.RoomNumber = longRoom;
        await McpWorkOrderSteps.Bus().Send(new SaveDraftCommand(legacy, legacy.Creator!)
        {
            PreviousRoomNumber = longRoom
        });

        await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creatorUserName,
            title: "Updated title");

        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number);
        reloaded!.RoomNumber!.Length.ShouldBe(WorkOrder.RoomNumberMaxLength);
        reloaded.RoomNumber.ShouldBe(longRoom);
    }

    [Test]
    public async Task ShouldFailForAssignedWorkOrderWithoutChangingFields()
    {
        var (creator, number) = await McpWorkOrderSteps.PersistAssignedSaveFixtureAsync();

        var result = await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            title: "New title");

        result.ShouldContain("cannot be executed");
        result.ShouldContain("Assigned");
    }

    [Test]
    public async Task ShouldLeaveAssignedFieldsUnchangedWhenSaveFails()
    {
        var (creator, number) = await McpWorkOrderSteps.PersistAssignedSaveFixtureAsync();

        await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            title: "New title");

        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number);
        reloaded!.Title.ShouldBe("Assigned title");
        reloaded.Description.ShouldBe("Assigned description");
        reloaded.Instructions.ShouldBe("Assigned instructions");
        reloaded.RoomNumber.ShouldBe("Room 1");
        reloaded.DueDate.ShouldBe(new DateOnly(2026, 9, 3));
        reloaded.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test]
    public async Task ShouldFailForCompleteWorkOrderWithoutChangingFields()
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Jane", "Smith");
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            creator,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Complete title")
                .CreatedBy(creator)
                .WithStatus(WorkOrderStatus.Complete)
                .Build());

        var result = await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            creator.UserName,
            title: "New title");

        result.ShouldContain("cannot be executed");
        result.ShouldContain("Complete");
        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number);
        reloaded!.Title.ShouldBe("Complete title");
        reloaded.Status.ShouldBe(WorkOrderStatus.Complete);
    }

    [Test]
    public async Task ShouldFailForNonCreatorWithoutChangingFields()
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Jane", "Smith");
        var otherUser = ObjectMother.Employee(TestData.UserName("other"), "Other", "User");
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            creator,
            otherUser,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Draft title")
                .CreatedBy(creator)
                .Build());

        var result = await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            otherUser.UserName,
            title: "New title");

        result.ShouldContain("cannot be executed");
        (await McpWorkOrderSteps.LoadByNumberAsync(number))!.Title.ShouldBe("Draft title");
    }

    [Test]
    public async Task ShouldFailWhenWorkOrderNumberMissing()
    {
        var result = await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            string.Empty,
            TestData.UserName("creator"),
            title: "New title");

        result.ShouldBe("Work order number is required.");
    }

    [Test]
    public async Task ShouldFailWhenWorkOrderNumberUnknown()
    {
        var creatorUserName = TestData.UserName("creator");
        await McpWorkOrderSteps.SeedCreatorAsync(creatorUserName);

        var result = await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            "UNKNOWN-WO",
            creatorUserName,
            title: "New title");

        result.ShouldBe("No work order found with number 'UNKNOWN-WO'.");
    }

    [Test]
    public async Task ShouldFailWhenExecutingUserUnknown()
    {
        var (_, number) = await McpWorkOrderSteps.CreateDraftViaToolAsync(new McpDraftCreate { Title = "Title", Description = "Description" });
        var missingUserName = TestData.UserName("missing");

        var result = await ProductionWorkOrderTools.SaveWorkOrder(
            McpWorkOrderSteps.Bus(),
            number,
            missingUserName,
            title: "New title");

        result.ShouldBe($"Employee with username '{missingUserName}' not found.");
        (await McpWorkOrderSteps.LoadByNumberAsync(number))!.Title.ShouldBe("Title");
    }

}
