using System.Reflection;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpSaveWorkOrderTests
{
    [Test]
    public async Task ShouldChangeTitleAndClearDueDateWhilePreservingOtherFields()
    {
        string creatorUserName = TestData.UserName("creator");
        await SeedCreatorAsync(creatorUserName);
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        const string instructions = "Check preschool gate latch before mowing";
        const string room = "Front lawn";

        string createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "mow front grass",
            "Weekly mowing",
            creatorUserName,
            roomNumber: room,
            dueDate: "2027-06-15",
            instructions: instructions);

        string? number = ExtractWorkOrderNumber(createResult);
        number.ShouldNotBeNullOrWhiteSpace();

        string saveResult = await WorkOrderTools.SaveWorkOrder(
            bus,
            number,
            creatorUserName,
            title: "Saturday mow",
            dueDate: string.Empty);

        saveResult.ShouldContain("Saturday mow");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded.ShouldNotBeNull();
        reloaded.Title.ShouldBe("Saturday mow");
        reloaded.DueDate.ShouldBeNull();
        reloaded.Instructions.ShouldBe(instructions);
        reloaded.RoomNumber.ShouldBe(room);
        reloaded.Status.ShouldBe(WorkOrderStatus.Draft);
        reloaded.Number.ShouldBe(number);
        reloaded.Creator!.UserName.ShouldBe(creatorUserName);
        reloaded.Assignee.ShouldBeNull();
    }

    [Test]
    public async Task ShouldLeaveOmittedFieldsUnchangedWhenOnlyRoomNumberProvided()
    {
        string creatorUserName = TestData.UserName("creator");
        await SeedCreatorAsync(creatorUserName);
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        const string title = "Original title";
        const string description = "Original description";
        const string instructions = "Original instructions";
        var dueDate = new DateOnly(2027, 6, 15);

        string createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            title,
            description,
            creatorUserName,
            roomNumber: "Room A",
            dueDate: dueDate.ToString("yyyy-MM-dd"),
            instructions: instructions);

        string number = ExtractWorkOrderNumber(createResult)!;

        await WorkOrderTools.SaveWorkOrder(bus, number, creatorUserName, roomNumber: "Room B");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.Title.ShouldBe(title);
        reloaded.Description.ShouldBe(description);
        reloaded.Instructions.ShouldBe(instructions);
        reloaded.DueDate.ShouldBe(dueDate);
        reloaded.RoomNumber.ShouldBe("Room B");
    }

    [Test]
    public async Task ShouldPersistEmptyDescriptionInstructionsAndRoom()
    {
        string creatorUserName = TestData.UserName("creator");
        await SeedCreatorAsync(creatorUserName);
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();

        string createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Has content",
            "Has description",
            creatorUserName,
            roomNumber: "Room 101",
            instructions: "Has instructions");

        string number = ExtractWorkOrderNumber(createResult)!;

        await WorkOrderTools.SaveWorkOrder(
            bus,
            number,
            creatorUserName,
            description: string.Empty,
            instructions: "   ",
            roomNumber: string.Empty);

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.Description.ShouldBe(string.Empty);
        reloaded.Instructions.ShouldBe(string.Empty);
        reloaded.RoomNumber.ShouldBe(string.Empty);
        reloaded.Title.ShouldBe("Has content");
    }

    [Test]
    public async Task ShouldPersistValidDueDate()
    {
        string creatorUserName = TestData.UserName("creator");
        await SeedCreatorAsync(creatorUserName);
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var expectedDueDate = new DateOnly(2026, 11, 15);

        string createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Due date save",
            "Description",
            creatorUserName);

        string number = ExtractWorkOrderNumber(createResult)!;

        await WorkOrderTools.SaveWorkOrder(
            bus,
            number,
            creatorUserName,
            dueDate: expectedDueDate.ToString("yyyy-MM-dd"));

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.DueDate.ShouldBe(expectedDueDate);
    }

    [Test]
    public async Task ShouldRejectInvalidDueDateWithoutChangingStoredDueDate()
    {
        string creatorUserName = TestData.UserName("creator");
        await SeedCreatorAsync(creatorUserName);
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var originalDueDate = new DateOnly(2027, 6, 15);

        string createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Invalid due date",
            "Description",
            creatorUserName,
            dueDate: originalDueDate.ToString("yyyy-MM-dd"));

        string number = ExtractWorkOrderNumber(createResult)!;

        string result = await WorkOrderTools.SaveWorkOrder(
            bus,
            number,
            creatorUserName,
            dueDate: "not-a-date");

        result.ShouldBe("Invalid due date 'not-a-date'. Use yyyy-MM-dd.");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.DueDate.ShouldBe(originalDueDate);
    }

    [Test]
    public async Task ShouldRejectEmptyTitleWithoutChangingStoredRow()
    {
        string creatorUserName = TestData.UserName("creator");
        await SeedCreatorAsync(creatorUserName);
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var originalDueDate = new DateOnly(2027, 6, 15);

        string createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Keep this title",
            "Description",
            creatorUserName,
            dueDate: originalDueDate.ToString("yyyy-MM-dd"));

        string number = ExtractWorkOrderNumber(createResult)!;

        string result = await WorkOrderTools.SaveWorkOrder(
            bus,
            number,
            creatorUserName,
            title: string.Empty);

        result.ShouldBe("The Title field is required.");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.Title.ShouldBe("Keep this title");
        reloaded.DueDate.ShouldBe(originalDueDate);
    }

    [Test]
    public async Task ShouldRejectWhitespaceTitleAndNotPersistDueDateInSameCall()
    {
        string creatorUserName = TestData.UserName("creator");
        await SeedCreatorAsync(creatorUserName);
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var originalDueDate = new DateOnly(2027, 6, 15);

        string createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Keep this title",
            "Description",
            creatorUserName,
            dueDate: originalDueDate.ToString("yyyy-MM-dd"));

        string number = ExtractWorkOrderNumber(createResult)!;

        string result = await WorkOrderTools.SaveWorkOrder(
            bus,
            number,
            creatorUserName,
            title: "   ",
            dueDate: "2026-12-25");

        result.ShouldBe("The Title field is required.");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.Title.ShouldBe("Keep this title");
        reloaded.DueDate.ShouldBe(originalDueDate);
    }

    [Test]
    public async Task ShouldTruncateInstructionsTo4000Characters()
    {
        string creatorUserName = TestData.UserName("creator");
        await SeedCreatorAsync(creatorUserName);
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        string longInstructions = new('x', WorkOrder.InstructionsMaxLength + 1);
        string expected = new('x', WorkOrder.InstructionsMaxLength);

        string createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Truncate instructions",
            "Description",
            creatorUserName);

        string number = ExtractWorkOrderNumber(createResult)!;

        string saveResult = await WorkOrderTools.SaveWorkOrder(
            bus,
            number,
            creatorUserName,
            instructions: longInstructions);

        saveResult.ShouldContain("Truncate instructions");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.Instructions!.Length.ShouldBe(WorkOrder.InstructionsMaxLength);
        reloaded.Instructions.ShouldBe(expected);
    }

    [Test]
    public async Task ShouldRejectRoomNumberLongerThan50CharactersOnNewWorkOrder()
    {
        string creatorUserName = TestData.UserName("creator");
        await SeedCreatorAsync(creatorUserName);
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        string tooLongRoom = new('r', WorkOrder.RoomNumberEntryMaxLength + 1);

        string createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Reject long room",
            "Description",
            creatorUserName);

        string number = ExtractWorkOrderNumber(createResult)!;

        string result = await WorkOrderTools.SaveWorkOrder(bus, number, creatorUserName, roomNumber: tooLongRoom);

        result.ShouldContain("cannot exceed");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.RoomNumber.ShouldBeNull();
    }

    [Test]
    public async Task ShouldAllowPreservingLongRoomNumberViaMcpOnExistingWorkOrder()
    {
        string creatorUserName = TestData.UserName("creator");
        await SeedCreatorAsync(creatorUserName);
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        string longRoom = new('r', WorkOrder.RoomNumberMaxLength);

        string createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Preserve long room",
            "Description",
            creatorUserName,
            roomNumber: null);

        string number = ExtractWorkOrderNumber(createResult)!;
        var legacy = (await bus.Send(new WorkOrderByNumberQuery(number)))!;
        legacy.RoomNumber = longRoom;
        await bus.Send(new SaveDraftCommand(legacy, legacy.Creator!) { PreviousRoomNumber = longRoom });

        await WorkOrderTools.SaveWorkOrder(bus, number, creatorUserName, title: "Updated title");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.RoomNumber!.Length.ShouldBe(WorkOrder.RoomNumberMaxLength);
        reloaded.RoomNumber.ShouldBe(longRoom);
    }

    [Test]
    public async Task ShouldFailForAssignedWorkOrderWithoutChangingFields()
    {
        string creatorUserName = TestData.UserName("creator");
        string workOrderNumber = TestData.Number("W");

        var creator = new Employee(creatorUserName, "Jane", "Smith", "jane@test.com");
        var assignedOrder = new WorkOrder
        {
            Creator = creator,
            Number = workOrderNumber,
            Title = "Assigned title",
            Description = "Assigned description",
            Instructions = "Assigned instructions",
            RoomNumber = "Room 1",
            DueDate = new DateOnly(2026, 9, 3),
            Status = WorkOrderStatus.Assigned
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(assignedOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        string result = await WorkOrderTools.SaveWorkOrder(
            bus,
            workOrderNumber,
            creatorUserName,
            title: "New title");

        result.ShouldContain("cannot be executed");
        result.ShouldContain("Assigned");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
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
        string creatorUserName = TestData.UserName("creator");
        string workOrderNumber = TestData.Number("W");

        var creator = new Employee(creatorUserName, "Jane", "Smith", "jane@test.com");
        var completeOrder = new WorkOrder
        {
            Creator = creator,
            Number = workOrderNumber,
            Title = "Complete title",
            Status = WorkOrderStatus.Complete
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(completeOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        string result = await WorkOrderTools.SaveWorkOrder(
            bus,
            workOrderNumber,
            creatorUserName,
            title: "New title");

        result.ShouldContain("cannot be executed");
        result.ShouldContain("Complete");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
        reloaded!.Title.ShouldBe("Complete title");
        reloaded.Status.ShouldBe(WorkOrderStatus.Complete);
    }

    [Test]
    public async Task ShouldFailForNonCreatorWithoutChangingFields()
    {
        string creatorUserName = TestData.UserName("creator");
        string otherUserName = TestData.UserName("other");
        string workOrderNumber = TestData.Number("W");

        var creator = new Employee(creatorUserName, "Jane", "Smith", "jane@test.com");
        var otherUser = new Employee(otherUserName, "Other", "User", "other@test.com");
        var draftOrder = new WorkOrder
        {
            Creator = creator,
            Number = workOrderNumber,
            Title = "Draft title",
            Status = WorkOrderStatus.Draft
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(otherUser);
        context.Add(draftOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        string result = await WorkOrderTools.SaveWorkOrder(
            bus,
            workOrderNumber,
            otherUserName,
            title: "New title");

        result.ShouldContain("cannot be executed");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
        reloaded!.Title.ShouldBe("Draft title");
    }

    [Test]
    public async Task ShouldFailWhenWorkOrderNumberMissing()
    {
        string creatorUserName = TestData.UserName("creator");

        var bus = TestHost.GetRequiredService<IBus>();
        string result = await WorkOrderTools.SaveWorkOrder(bus, string.Empty, creatorUserName, title: "New title");
        result.ShouldBe("Work order number is required.");
    }

    [Test]
    public async Task ShouldFailWhenWorkOrderNumberUnknown()
    {
        string creatorUserName = TestData.UserName("creator");
        await SeedCreatorAsync(creatorUserName);
        var bus = TestHost.GetRequiredService<IBus>();
        string result = await WorkOrderTools.SaveWorkOrder(bus, "UNKNOWN-WO", creatorUserName, title: "New title");
        result.ShouldBe("No work order found with number 'UNKNOWN-WO'.");
    }

    [Test]
    public async Task ShouldFailWhenExecutingUserUnknown()
    {
        string creatorUserName = TestData.UserName("creator");
        string missingUserName = TestData.UserName("missing");

        await SeedCreatorAsync(creatorUserName);
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        string createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Title",
            "Description",
            creatorUserName);
        string number = ExtractWorkOrderNumber(createResult)!;

        string result = await WorkOrderTools.SaveWorkOrder(bus, number, missingUserName, title: "New title");
        result.ShouldBe($"Employee with username '{missingUserName}' not found.");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery(number));
        reloaded!.Title.ShouldBe("Title");
    }

    [Test]
    public async Task ShouldRejectDescriptionLongerThan4000CharactersWhenCreatingWorkOrder()
    {
        string creatorUserName = TestData.UserName("creator");
        await SeedCreatorAsync(creatorUserName);
        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        string tooLongDescription = new('d', WorkOrder.DescriptionMaxLength + 1);

        string result = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Title",
            tooLongDescription,
            creatorUserName);

        result.ShouldBe("Description cannot exceed 4000 characters.");
    }

    [Test]
    public void ShouldNotExposeSaveOnExecuteWorkOrderCommand()
    {
        var executorType = typeof(WorkOrderCommandExecutor);
        var field = executorType.GetField("CommandFactories", BindingFlags.NonPublic | BindingFlags.Static);
        field.ShouldNotBeNull();

        object? fieldValue = field.GetValue(null);
        fieldValue.ShouldNotBeNull();

        var factories = (IReadOnlyDictionary<string, Func<WorkOrder, Employee, StateCommandBase>>)fieldValue;
        factories.Keys.ShouldNotContain("Save");
        factories.Keys.ShouldNotContain("SaveDraftCommand");
        factories.Keys.ShouldNotContain(SaveDraftCommand.Name);
    }

    private static async Task SeedCreatorAsync(string userName)
    {
        var employee = new Employee(userName, "Jane", "Smith", "jane@test.com");
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        await context.SaveChangesAsync();
    }

    private static string? ExtractWorkOrderNumber(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("Number").GetString();
    }
}
