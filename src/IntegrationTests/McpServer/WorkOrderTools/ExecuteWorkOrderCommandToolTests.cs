using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UnitTests;
using Shouldly;
using ProductionWorkOrderTools = ClearMeasure.Bootcamp.McpServer.Tools.WorkOrderTools;
using TestWorkOrderBuilder = ClearMeasure.Bootcamp.UnitTests.TestSupport.WorkOrderBuilder;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer.WorkOrderTools;

[TestFixture]
public class ExecuteWorkOrderCommandToolTests
{
    [Test]
    public async Task ShouldReturnErrorForUnknownCommand()
    {
        var employee = ObjectMother.Employee(TestData.UserName("user"), "John", "Doe");
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            employee,
            TestWorkOrderBuilder.Create().WithNumber(number).WithTitle("Test").CreatedBy(employee).Build());

        var result = await ProductionWorkOrderTools.ExecuteWorkOrderCommand(
            McpWorkOrderSteps.Bus(),
            number,
            "FakeCommand",
            employee.UserName);

        result.ShouldContain("Unknown command");
        result.ShouldContain("Available commands");
    }

    [Test]
    public async Task ShouldExecuteCancelCommand()
    {
        var employee = ObjectMother.Employee(TestData.UserName("user"), "John", "Doe");
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            employee,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Assigned order")
                .CreatedBy(employee)
                .WithStatus(WorkOrderStatus.Assigned)
                .Build());

        var result = await ProductionWorkOrderTools.ExecuteWorkOrderCommand(
            McpWorkOrderSteps.Bus(),
            number,
            "AssignedToCancelledCommand",
            employee.UserName);

        (await McpWorkOrderSteps.RequirePersistedAsync(number)).Status.ShouldBe(WorkOrderStatus.Cancelled);
        result.ShouldContain("Cancelled");
    }

    [Test]
    public async Task ShouldExecuteShelveCommand()
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Timothy", "Lovejoy");
        var assignee = ObjectMother.Employee(TestData.UserName("gw"), "Groundskeeper Willie", "MacDougal");
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            creator,
            assignee,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Mow grass")
                .CreatedBy(creator)
                .AssignedTo(assignee)
                .WithStatus(WorkOrderStatus.InProgress)
                .Build());

        var result = await ProductionWorkOrderTools.ExecuteWorkOrderCommand(
            McpWorkOrderSteps.Bus(),
            number,
            "InProgressToAssignedCommand",
            assignee.UserName,
            reason: "Waiting on parts");

        var wo = await McpWorkOrderSteps.RequirePersistedAsync(number);
        wo.Status.ShouldBe(WorkOrderStatus.Assigned);
        result.ShouldContain("Assigned");
        (await McpWorkOrderSteps.RequireStatusChangeAsync(wo.Id)).Reason.ShouldBe("Waiting on parts");
    }

    [Test]
    public async Task ShouldReturnErrorWhenDraftToAssignedMissingAssignee()
    {
        var creator = ObjectMother.Creator(TestData.UserName("creator"));
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            creator,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Needs assignee")
                .CreatedBy(creator)
                .Build());

        var result = await ProductionWorkOrderTools.ExecuteWorkOrderCommand(
            McpWorkOrderSteps.Bus(),
            number,
            "DraftToAssignedCommand",
            creator.UserName);

        result.ShouldContain("requires an assigneeUsername");
    }

    [Test]
    public async Task ShouldExecuteShelveAliasCommand()
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Timothy", "Lovejoy");
        var assignee = ObjectMother.Fulfiller(TestData.UserName("gw"));
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            creator,
            assignee,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Shelve alias")
                .CreatedBy(creator)
                .AssignedTo(assignee)
                .WithStatus(WorkOrderStatus.InProgress)
                .Build());

        var result = await ProductionWorkOrderTools.ExecuteWorkOrderCommand(
            McpWorkOrderSteps.Bus(),
            number,
            "Shelve",
            assignee.UserName,
            reason: "Waiting on parts");

        var wo = await McpWorkOrderSteps.RequirePersistedAsync(number);
        wo.Status.ShouldBe(WorkOrderStatus.Assigned);
        result.ShouldContain("Assigned");
        (await McpWorkOrderSteps.RequireStatusChangeAsync(wo.Id)).Reason.ShouldBe("Waiting on parts");
    }

    [Test]
    public async Task ShouldRefuseShelveWithoutReason()
    {
        var assignee = ObjectMother.Employee(TestData.UserName("gw"), "Groundskeeper Willie", "MacDougal");
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            assignee,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Needs a reason")
                .CreatedBy(assignee)
                .AssignedTo(assignee)
                .WithStatus(WorkOrderStatus.InProgress)
                .Build());
        var bus = McpWorkOrderSteps.Bus();

        var missing = await ProductionWorkOrderTools.ExecuteWorkOrderCommand(bus, number, "Shelve", assignee.UserName);
        var tooLong = await ProductionWorkOrderTools.ExecuteWorkOrderCommand(
            bus,
            number,
            "Shelve",
            assignee.UserName,
            reason: new string('x', InProgressToAssignedCommand.ReasonMaxLength + 1));

        missing.ShouldBe("Shelve requires a reason.");
        tooLong.ShouldBe($"Reason cannot exceed {InProgressToAssignedCommand.ReasonMaxLength} characters.");
        (await McpWorkOrderSteps.RequirePersistedAsync(number)).Status.ShouldBe(WorkOrderStatus.InProgress);
    }

    [Test]
    public async Task ShouldExecuteDraftToAssignedThenBeginStatusChanges()
    {
        var creator = ObjectMother.Creator(TestData.UserName("creator"));
        var assignee = ObjectMother.Fulfiller(TestData.UserName("worker"));
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            creator,
            assignee,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Status flow")
                .CreatedBy(creator)
                .Build());
        var bus = McpWorkOrderSteps.Bus();

        var assignResult = await ProductionWorkOrderTools.ExecuteWorkOrderCommand(
            bus, number, "DraftToAssignedCommand", creator.UserName, assignee.UserName);
        var beginResult = await ProductionWorkOrderTools.ExecuteWorkOrderCommand(
            bus, number, "AssignedToInProgressCommand", assignee.UserName);

        assignResult.ShouldContain("Assigned");
        beginResult.ShouldContain("In Progress");
        var wo = await McpWorkOrderSteps.RequirePersistedAsync(number);
        wo.Status.ShouldBe(WorkOrderStatus.InProgress);
        wo.Assignee!.UserName.ShouldBe(assignee.UserName);
    }

    [Test]
    public async Task ShouldReturnNotFoundWhenExecutingWorkOrderCommandForMissingWorkOrder()
    {
        var result = await ProductionWorkOrderTools.ExecuteWorkOrderCommand(
            McpWorkOrderSteps.Bus(),
            "MISSING-WO",
            "AssignedToCancelledCommand",
            TestData.UserName("user"));

        result.ShouldContain("No work order found");
    }

    [Test]
    public async Task ShouldReturnNotFoundWhenExecutingUserMissing()
    {
        var employee = ObjectMother.Employee(TestData.UserName("user"), "John", "Doe");
        var number = TestData.Number("W");
        var unknownUserName = TestData.UserName("ghost");
        await McpWorkOrderSteps.PersistAsync(
            employee,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Test")
                .CreatedBy(employee)
                .WithStatus(WorkOrderStatus.Assigned)
                .Build());

        var result = await ProductionWorkOrderTools.ExecuteWorkOrderCommand(
            McpWorkOrderSteps.Bus(),
            number,
            "AssignedToCancelledCommand",
            unknownUserName);

        result.ShouldContain($"Employee with username '{unknownUserName}' not found");
    }

    [Test]
    public async Task ShouldReturnNotFoundWhenAssigneeMissingForDraftToAssigned()
    {
        var creator = ObjectMother.Creator(TestData.UserName("creator"));
        var number = TestData.Number("W");
        var missingAssigneeUserName = TestData.UserName("missing");
        await McpWorkOrderSteps.PersistAsync(
            creator,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Missing assignee")
                .CreatedBy(creator)
                .Build());

        var result = await ProductionWorkOrderTools.ExecuteWorkOrderCommand(
            McpWorkOrderSteps.Bus(),
            number,
            "DraftToAssignedCommand",
            creator.UserName,
            missingAssigneeUserName);

        result.ShouldContain($"Assignee with username '{missingAssigneeUserName}' not found");
    }

    [Test]
    public async Task ShouldReturnErrorWhenCommandInvalidForCurrentStatus()
    {
        var employee = ObjectMother.Employee(TestData.UserName("user"), "John", "Doe");
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            employee,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Draft")
                .CreatedBy(employee)
                .Build());

        var result = await ProductionWorkOrderTools.ExecuteWorkOrderCommand(
            McpWorkOrderSteps.Bus(),
            number,
            "AssignedToCancelledCommand",
            employee.UserName);

        result.ShouldContain("cannot be executed");
        result.ShouldContain("Draft");
    }

    [Test]
    public async Task ShouldExecuteInProgressToCompleteCommand()
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Jane", "Creator");
        var assignee = ObjectMother.Fulfiller(TestData.UserName("worker"));
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            creator,
            assignee,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Complete me")
                .CreatedBy(creator)
                .AssignedTo(assignee)
                .WithStatus(WorkOrderStatus.InProgress)
                .Build());

        var result = await ProductionWorkOrderTools.ExecuteWorkOrderCommand(
            McpWorkOrderSteps.Bus(),
            number,
            "InProgressToCompleteCommand",
            assignee.UserName);

        result.ShouldContain("Complete");
        (await McpWorkOrderSteps.RequirePersistedAsync(number)).Status.ShouldBe(WorkOrderStatus.Complete);
    }
}
