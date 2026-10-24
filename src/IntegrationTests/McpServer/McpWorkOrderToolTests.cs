using System.Reflection;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpWorkOrderToolTests
{
    [Test]
    public async Task ShouldListAllWorkOrders()
    {
        var employeeUserName = TestData.UserName("user");
        var number1 = TestData.Number("W");
        var number2 = TestData.Number("W");

        var employee = new Employee(employeeUserName, "John", "Doe", "john@test.com");
        var order1 = new WorkOrder { Creator = employee, Number = number1, Title = "Fix sink" };
        var order2 = new WorkOrder { Creator = employee, Number = number2, Title = "Paint wall" };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        context.Add(order1);
        context.Add(order2);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ListWorkOrders(bus);

        result.ShouldContain(number1);
        result.ShouldContain(number2);
        result.ShouldContain("Fix sink");
        result.ShouldContain("Paint wall");
    }

    [Test]
    public async Task ShouldFilterWorkOrdersByStatus()
    {
        var employeeUserName = TestData.UserName("user");
        var number1 = TestData.Number("W");
        var number2 = TestData.Number("W");

        var employee = new Employee(employeeUserName, "John", "Doe", "john@test.com");
        var draftOrder = new WorkOrder { Creator = employee, Number = number1, Title = "Draft order", Status = WorkOrderStatus.Draft };
        var assignedOrder = new WorkOrder { Creator = employee, Number = number2, Title = "Assigned order", Status = WorkOrderStatus.Assigned };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        context.Add(draftOrder);
        context.Add(assignedOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ListWorkOrders(bus, "Assigned");

        result.ShouldContain(number2);
        result.ShouldNotContain(number1);
    }

    [Test]
    public async Task ShouldFilterWorkOrdersByCreatorUsername()
    {
        var seed = await SeedFilterWorkOrders();
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await WorkOrderTools.ListWorkOrders(bus, creatorUsername: seed.CreatorUserName);

        ExtractNumbers(result).ShouldBeSet(
            seed["LJDRAFT"],
            seed["LJWA"],
            seed["LJWIP"],
            seed["LJCOMP"]);
    }

    [Test]
    public async Task ShouldFilterWorkOrdersByAssigneeUsername()
    {
        var seed = await SeedFilterWorkOrders();
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await WorkOrderTools.ListWorkOrders(bus, assigneeUsername: seed.AssigneeUserName);

        ExtractNumbers(result).ShouldBeSet(
            seed["LJWA"],
            seed["LJWIP"],
            seed["OTWIP"]);
    }

    [Test]
    public async Task ShouldAndAssigneeAndStatusFilters()
    {
        var seed = await SeedFilterWorkOrders();
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await WorkOrderTools.ListWorkOrders(
            bus,
            status: "InProgress",
            assigneeUsername: seed.AssigneeUserName);

        ExtractNumbers(result).ShouldBeSet(
            seed["LJWIP"],
            seed["OTWIP"]);
    }

    [Test]
    public async Task ShouldAndCreatorAndStatusFiltersForLovejoyDrafts()
    {
        var seed = await SeedFilterWorkOrders();
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await WorkOrderTools.ListWorkOrders(
            bus,
            status: "Draft",
            creatorUsername: seed.CreatorUserName);

        ExtractNumbers(result).ShouldBeSet(seed["LJDRAFT"]);
    }

    [Test]
    public async Task ShouldAndCreatorAssigneeAndStatusFilters()
    {
        var seed = await SeedFilterWorkOrders();
        var bus = TestHost.GetRequiredService<IBus>();

        var result = await WorkOrderTools.ListWorkOrders(
            bus,
            status: "InProgress",
            creatorUsername: seed.CreatorUserName,
            assigneeUsername: seed.AssigneeUserName);

        ExtractNumbers(result).ShouldBeSet(seed["LJWIP"]);
    }

    [Test]
    public async Task ShouldTreatOmittedEmptyAndWhitespaceFiltersAsDisabled()
    {
        var seed = await SeedFilterWorkOrders();
        var bus = TestHost.GetRequiredService<IBus>();

        var omitted = ExtractNumbers(await WorkOrderTools.ListWorkOrders(bus));
        var blank = ExtractNumbers(await WorkOrderTools.ListWorkOrders(bus, " ", string.Empty, "   "));

        omitted.Where(seed.AllNumbers.Contains).ShouldBeSet(seed.AllNumbers);
        blank.ShouldBeSet(omitted);
    }

    [Test]
    public async Task ShouldReturnEmptyForUnknownCreatorOrAssigneeWithoutCreatingEmployee()
    {
        var seed = await SeedFilterWorkOrders();
        var bus = TestHost.GetRequiredService<IBus>();
        await using var context = TestHost.GetRequiredService<DbContext>();
        var employeeCount = await context.Set<Employee>().CountAsync();

        var unknownCreator = await WorkOrderTools.ListWorkOrders(
            bus,
            status: "Assigned",
            creatorUsername: TestData.UserName("not-a-person"));
        var unknownAssignee = await WorkOrderTools.ListWorkOrders(
            bus,
            status: "Draft",
            creatorUsername: seed.CreatorUserName,
            assigneeUsername: TestData.UserName("not-a-person"));

        unknownCreator.ShouldBe("[]");
        unknownAssignee.ShouldBe("[]");
        await using var verificationContext = TestHost.GetRequiredService<DbContext>();
        (await verificationContext.Set<Employee>().CountAsync()).ShouldBe(employeeCount);
    }

    [Test]
    public async Task ShouldPreserveInvalidStatusFailure()
    {
        var bus = TestHost.GetRequiredService<IBus>();

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => WorkOrderTools.ListWorkOrders(bus, status: "NotAStatus"));
    }

    [Test]
    public async Task ShouldGetWorkOrderByNumberWithCompleteDetailShape()
    {
        var employeeUserName = TestData.UserName("user");
        var workOrderNumber = TestData.Number("W");

        var employee = new Employee(employeeUserName, "John", "Doe", "john@test.com");
        var createdDate = new DateTime(2026, 8, 25, 14, 30, 0, DateTimeKind.Utc);
        var order = new WorkOrder
        {
            Creator = employee,
            Number = workOrderNumber,
            Title = "Test order",
            Description = "A description",
            Instructions = "preschool quiet",
            RoomNumber = "101",
            DueDate = new DateOnly(2026, 9, 12),
            Status = WorkOrderStatus.Draft,
            CreatedDate = createdDate,
            Priority = WorkOrderPriority.High,
            Building = "Main Hall"
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        context.Add(order);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.GetWorkOrder(bus, workOrderNumber);

        using var document = JsonDocument.Parse(result);
        var detail = document.RootElement;
        detail.GetProperty("Number").GetString().ShouldBe(workOrderNumber);
        detail.GetProperty("Title").GetString().ShouldBe("Test order");
        detail.GetProperty("Description").GetString().ShouldBe("A description");
        detail.GetProperty("Instructions").GetString().ShouldBe("preschool quiet");
        detail.GetProperty("RoomNumber").GetString().ShouldBe("101");
        detail.GetProperty("DueDate").GetString().ShouldBe("2026-09-12");
        detail.GetProperty("Status").GetString().ShouldBe("Draft");
        detail.GetProperty("Creator").GetString().ShouldBe("John Doe");
        detail.GetProperty("CreatorUsername").GetString().ShouldBe(employeeUserName);
        detail.GetProperty("Assignee").ValueKind.ShouldBe(JsonValueKind.Null);
        detail.GetProperty("AssigneeUsername").ValueKind.ShouldBe(JsonValueKind.Null);
        detail.GetProperty("CreatedDate").GetDateTime().ShouldBe(createdDate);
        detail.GetProperty("AssignedDate").ValueKind.ShouldBe(JsonValueKind.Null);
        detail.GetProperty("CompletedDate").ValueKind.ShouldBe(JsonValueKind.Null);
        detail.GetProperty("Priority").GetString().ShouldBe("High");
        detail.GetProperty("Building").GetString().ShouldBe("Main Hall");
    }

    [Test]
    public async Task ShouldGetWorkOrderWithEmptyInstructionsPropertyWhenInstructionsOmitted()
    {
        var employeeUserName = TestData.UserName("user");
        var workOrderNumber = TestData.Number("W");

        var employee = new Employee(employeeUserName, "John", "Doe", "john@test.com");
        var order = new WorkOrder
        {
            Creator = employee,
            Number = workOrderNumber,
            Title = "No special instructions",
            Status = WorkOrderStatus.Draft
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        context.Add(order);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.GetWorkOrder(bus, workOrderNumber);

        using var document = JsonDocument.Parse(result);
        var instructions = document.RootElement.GetProperty("Instructions");
        instructions.ValueKind.ShouldBe(JsonValueKind.String);
        instructions.GetString().ShouldBe(string.Empty);
        result.ShouldContain("\"Instructions\": \"\"");
        result.ShouldNotContain("\"Instructions\": \"null\"");
    }

    [Test]
    public async Task ShouldReturnNotFoundForMissingWorkOrder()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.GetWorkOrder(bus, "NONEXISTENT");

        result.ShouldBe("No work order found with number 'NONEXISTENT'.");
    }

    [Test]
    public async Task ShouldKeepListWorkOrderSummaryWithoutInstructions()
    {
        var employeeUserName = TestData.UserName("user");
        var workOrderNumber = TestData.Number("W");

        var employee = new Employee(employeeUserName, "John", "Doe", "john@test.com");
        var order = new WorkOrder
        {
            Creator = employee,
            Number = workOrderNumber,
            Title = "Summary shape",
            Instructions = "detail only"
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        context.Add(order);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ListWorkOrders(bus, creatorUsername: employeeUserName);

        using var document = JsonDocument.Parse(result);
        var summary = document.RootElement.EnumerateArray().Single();
        summary.TryGetProperty("Instructions", out _).ShouldBeFalse();
    }

    [Test]
    public void ShouldKeepWorkOrderToolArgumentListsBackwardCompatible()
    {
        ParameterNames(nameof(WorkOrderTools.CreateWorkOrder)).ShouldBe(
            ["bus", "numberGenerator", "title", "description", "creatorUsername", "roomNumber", "dueDate", "instructions", "priority", "building"]);
        ParameterNames(nameof(WorkOrderTools.SaveWorkOrder)).ShouldBe(
            ["bus", "workOrderNumber", "executingUsername", "title", "description", "instructions", "roomNumber", "dueDate"]);
        ParameterNames(nameof(WorkOrderTools.CreateDatedWorkOrders)).ShouldBe(
            ["bus", "timeProvider", "creatorUsername", "assigneeUsername", "title", "description", "dueDates", "saturdayCount"]);
    }

    [Test]
    public async Task ShouldCreateDraftWorkOrder()
    {
        var creatorUserName = TestData.UserName("creator");

        var employee = new Employee(creatorUserName, "Jane", "Smith", "jane@test.com");

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var result = await WorkOrderTools.CreateWorkOrder(bus, numberGenerator, "New Work Order", "Fix the broken window", creatorUserName);

        result.ShouldContain("New Work Order");
        result.ShouldContain("Fix the broken window");
        result.ShouldContain("Draft");
    }

    [Test]
    public async Task ShouldReturnErrorForMissingCreator()
    {
        var missingUserName = TestData.UserName("missing");

        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var result = await WorkOrderTools.CreateWorkOrder(bus, numberGenerator, "Title", "Description", missingUserName);

        result.ShouldContain("not found");
    }

    [Test]
    public async Task ShouldReturnErrorForUnknownCommand()
    {
        var employeeUserName = TestData.UserName("user");
        var workOrderNumber = TestData.Number("W");

        var employee = new Employee(employeeUserName, "John", "Doe", "john@test.com");
        var order = new WorkOrder { Creator = employee, Number = workOrderNumber, Title = "Test" };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        context.Add(order);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(bus, workOrderNumber, "FakeCommand", employeeUserName);

        result.ShouldContain("Unknown command");
        result.ShouldContain("Available commands");
    }

    [Test]
    public async Task ShouldExecuteCancelCommand()
    {
        var employeeUserName = TestData.UserName("user");
        var number2 = TestData.Number("W");

        var employee = new Employee(employeeUserName, "John", "Doe", "john@test.com");
        var assignedOrder = new WorkOrder { Creator = employee, Number = number2, Title = "Assigned order", Status = WorkOrderStatus.Assigned };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        context.Add(assignedOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(bus, number2, "AssignedToCancelledCommand", employeeUserName);

        await using var context2 = TestHost.GetRequiredService<DbContext>();
        var wo = context2.Set<WorkOrder>().Single(w => w.Number == number2);

        wo.Status.ShouldBe(WorkOrderStatus.Cancelled);
        result.ShouldContain("Cancelled");
    }

    [Test]
    public async Task ShouldExecuteShelveCommand()
    {
        var creatorUserName = TestData.UserName("creator");
        var assigneeUserName = TestData.UserName("gw");
        var workOrderNumber = TestData.Number("W");

        var creator = new Employee(creatorUserName, "Timothy", "Lovejoy", "timothy@test.com");
        var assignee = new Employee(assigneeUserName, "Groundskeeper Willie", "MacDougal", "willie@test.com");
        var inProgressOrder = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = workOrderNumber,
            Title = "Mow grass",
            Status = WorkOrderStatus.InProgress
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(assignee);
        context.Add(inProgressOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(
            bus, workOrderNumber, "InProgressToAssignedCommand", assigneeUserName, reason: "Waiting on parts");

        await using var context2 = TestHost.GetRequiredService<DbContext>();
        var wo = context2.Set<WorkOrder>().Single(w => w.Number == workOrderNumber);

        wo.Status.ShouldBe(WorkOrderStatus.Assigned);
        result.ShouldContain("Assigned");
        var change = context2.Set<WorkOrderStatusChange>().Single(row => row.WorkOrderId == wo.Id);
        change.Reason.ShouldBe("Waiting on parts");
    }

    [Test]
    public async Task ShouldReturnErrorWhenDraftToAssignedMissingAssignee()
    {
        var creatorUserName = TestData.UserName("creator");
        var workOrderNumber = TestData.Number("W");

        var creator = new Employee(creatorUserName, "Jane", "Creator", "creator@test.com");
        creator.AddRole(new Role($"Manager_{TestData.Tag()}", true, false));
        var draftOrder = new WorkOrder
        {
            Creator = creator,
            Number = workOrderNumber,
            Title = "Needs assignee",
            Status = WorkOrderStatus.Draft
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(draftOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(bus, workOrderNumber, "DraftToAssignedCommand", creatorUserName);

        result.ShouldContain("requires an assigneeUsername");
    }

    [Test]
    public async Task ShouldExecuteShelveAliasCommand()
    {
        var creatorUserName = TestData.UserName("creator");
        var assigneeUserName = TestData.UserName("gw");
        var workOrderNumber = TestData.Number("W");

        var creator = new Employee(creatorUserName, "Timothy", "Lovejoy", "timothy@test.com");
        var assignee = new Employee(assigneeUserName, "Groundskeeper Willie", "MacDougal", "willie@test.com");
        assignee.AddRole(new Role($"Worker_{TestData.Tag()}", false, true));
        var inProgressOrder = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = workOrderNumber,
            Title = "Shelve alias",
            Status = WorkOrderStatus.InProgress
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(assignee);
        context.Add(inProgressOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(
            bus, workOrderNumber, "Shelve", assigneeUserName, reason: "Waiting on parts");

        await using var context2 = TestHost.GetRequiredService<DbContext>();
        var wo = await context2.Set<WorkOrder>().SingleAsync(w => w.Number == workOrderNumber);

        wo.Status.ShouldBe(WorkOrderStatus.Assigned);
        result.ShouldContain("Assigned");
        var change = context2.Set<WorkOrderStatusChange>().Single(row => row.WorkOrderId == wo.Id);
        change.Reason.ShouldBe("Waiting on parts");
    }

    [Test]
    public async Task ShouldRefuseShelveWithoutReason()
    {
        var assigneeUserName = TestData.UserName("gw");
        var workOrderNumber = TestData.Number("W");
        var assignee = new Employee(assigneeUserName, "Groundskeeper Willie", "MacDougal", "willie@test.com");
        var inProgressOrder = new WorkOrder
        {
            Creator = assignee,
            Assignee = assignee,
            Number = workOrderNumber,
            Title = "Needs a reason",
            Status = WorkOrderStatus.InProgress
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(assignee);
        context.Add(inProgressOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var missing = await WorkOrderTools.ExecuteWorkOrderCommand(bus, workOrderNumber, "Shelve", assigneeUserName);
        var tooLong = await WorkOrderTools.ExecuteWorkOrderCommand(
            bus,
            workOrderNumber,
            "Shelve",
            assigneeUserName,
            reason: new string('x', InProgressToAssignedCommand.ReasonMaxLength + 1));

        missing.ShouldBe("Shelve requires a reason.");
        tooLong.ShouldBe($"Reason cannot exceed {InProgressToAssignedCommand.ReasonMaxLength} characters.");

        await using var context2 = TestHost.GetRequiredService<DbContext>();
        var wo = context2.Set<WorkOrder>().Single(w => w.Number == workOrderNumber);
        wo.Status.ShouldBe(WorkOrderStatus.InProgress);
    }

    [Test]
    public async Task ShouldExecuteDraftToAssignedThenBeginStatusChanges()
    {
        var creatorUserName = TestData.UserName("creator");
        var workerUserName = TestData.UserName("worker");
        var workOrderNumber = TestData.Number("W");

        var creator = new Employee(creatorUserName, "Jane", "Creator", "creator@test.com");
        creator.AddRole(new Role($"Manager_{TestData.Tag()}", true, false));
        var assignee = new Employee(workerUserName, "Sam", "Worker", "worker@test.com");
        assignee.AddRole(new Role($"Worker_{TestData.Tag()}", false, true));
        var draftOrder = new WorkOrder
        {
            Creator = creator,
            Number = workOrderNumber,
            Title = "Status flow",
            Status = WorkOrderStatus.Draft
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(assignee);
        context.Add(draftOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var assignResult = await WorkOrderTools.ExecuteWorkOrderCommand(
            bus,
            workOrderNumber,
            "DraftToAssignedCommand",
            creatorUserName,
            workerUserName);

        assignResult.ShouldContain("Assigned");

        var beginResult = await WorkOrderTools.ExecuteWorkOrderCommand(
            bus,
            workOrderNumber,
            "AssignedToInProgressCommand",
            workerUserName);

        beginResult.ShouldContain("In Progress");

        await using var context2 = TestHost.GetRequiredService<DbContext>();
        var wo = await context2.Set<WorkOrder>().SingleAsync(w => w.Number == workOrderNumber);

        wo.Status.ShouldBe(WorkOrderStatus.InProgress);
        wo.Assignee!.UserName.ShouldBe(workerUserName);
    }

    [Test]
    public async Task ShouldReturnNotFoundWhenExecutingWorkOrderCommandForMissingWorkOrder()
    {
        var employeeUserName = TestData.UserName("user");

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(bus, "MISSING-WO", "AssignedToCancelledCommand", employeeUserName);

        result.ShouldContain("No work order found");
    }

    [Test]
    public async Task ShouldReturnNotFoundWhenExecutingUserMissing()
    {
        var employeeUserName = TestData.UserName("user");
        var workOrderNumber = TestData.Number("W");
        var unknownUserName = TestData.UserName("ghost");

        var employee = new Employee(employeeUserName, "John", "Doe", "john@test.com");
        var order = new WorkOrder { Creator = employee, Number = workOrderNumber, Title = "Test", Status = WorkOrderStatus.Assigned };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        context.Add(order);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(bus, workOrderNumber, "AssignedToCancelledCommand", unknownUserName);

        result.ShouldContain($"Employee with username '{unknownUserName}' not found");
    }

    [Test]
    public async Task ShouldReturnNotFoundWhenAssigneeMissingForDraftToAssigned()
    {
        var creatorUserName = TestData.UserName("creator");
        var workOrderNumber = TestData.Number("W");
        var missingAssigneeUserName = TestData.UserName("missing");

        var creator = new Employee(creatorUserName, "Jane", "Creator", "creator@test.com");
        creator.AddRole(new Role($"Manager_{TestData.Tag()}", true, false));
        var draftOrder = new WorkOrder
        {
            Creator = creator,
            Number = workOrderNumber,
            Title = "Missing assignee",
            Status = WorkOrderStatus.Draft
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(draftOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(
            bus,
            workOrderNumber,
            "DraftToAssignedCommand",
            creatorUserName,
            missingAssigneeUserName);

        result.ShouldContain($"Assignee with username '{missingAssigneeUserName}' not found");
    }

    [Test]
    public async Task ShouldReturnErrorWhenCommandInvalidForCurrentStatus()
    {
        var employeeUserName = TestData.UserName("user");
        var workOrderNumber = TestData.Number("W");

        var employee = new Employee(employeeUserName, "John", "Doe", "john@test.com");
        var draftOrder = new WorkOrder { Creator = employee, Number = workOrderNumber, Title = "Draft", Status = WorkOrderStatus.Draft };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        context.Add(draftOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(bus, workOrderNumber, "AssignedToCancelledCommand", employeeUserName);

        result.ShouldContain("cannot be executed");
        result.ShouldContain("Draft");
    }

    [Test]
    public async Task ShouldExecuteInProgressToCompleteCommand()
    {
        var creatorUserName = TestData.UserName("creator");
        var workerUserName = TestData.UserName("worker");
        var workOrderNumber = TestData.Number("W");

        var creator = new Employee(creatorUserName, "Jane", "Creator", "creator@test.com");
        var assignee = new Employee(workerUserName, "Sam", "Worker", "worker@test.com");
        assignee.AddRole(new Role($"Worker_{TestData.Tag()}", false, true));
        var inProgressOrder = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = workOrderNumber,
            Title = "Complete me",
            Status = WorkOrderStatus.InProgress
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(assignee);
        context.Add(inProgressOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ExecuteWorkOrderCommand(
            bus,
            workOrderNumber,
            "InProgressToCompleteCommand",
            workerUserName);

        result.ShouldContain("Complete");

        await using var context2 = TestHost.GetRequiredService<DbContext>();
        var wo = await context2.Set<WorkOrder>().SingleAsync(w => w.Number == workOrderNumber);

        wo.Status.ShouldBe(WorkOrderStatus.Complete);
    }

    /// <summary>
    /// Seeds two creators and two assignees with unique user names and seven work orders whose
    /// unique numbers are looked up by their role in the filter tests (for example "LJWA": the
    /// creator's order assigned to the assignee).
    /// </summary>
    private static async Task<FilterSeed> SeedFilterWorkOrders()
    {
        var creator = new Employee(TestData.UserName("creator"), "Filter", "Creator", "filter-creator@test.com");
        var otherCreator = new Employee(TestData.UserName("other-creator"), "Other", "Creator", "creator@test.com");
        var assignee = new Employee(TestData.UserName("assignee"), "Filter", "Assignee", "filter-assignee@test.com");
        var otherAssignee = new Employee(TestData.UserName("other-assignee"), "Other", "Assignee", "assignee@test.com");
        var numbers = new[] { "LJDRAFT", "OTDRAFT", "LJWA", "LJWIP", "OTWIP", "LJCOMP", "OTASN" }
            .ToDictionary(key => key, _ => TestData.Number("F"));
        var workOrders = new[]
        {
            CreateWorkOrder(numbers["LJDRAFT"], creator, null, WorkOrderStatus.Draft),
            CreateWorkOrder(numbers["OTDRAFT"], otherCreator, null, WorkOrderStatus.Draft),
            CreateWorkOrder(numbers["LJWA"], creator, assignee, WorkOrderStatus.Assigned),
            CreateWorkOrder(numbers["LJWIP"], creator, assignee, WorkOrderStatus.InProgress),
            CreateWorkOrder(numbers["OTWIP"], otherCreator, assignee, WorkOrderStatus.InProgress),
            CreateWorkOrder(numbers["LJCOMP"], creator, otherAssignee, WorkOrderStatus.Complete),
            CreateWorkOrder(numbers["OTASN"], otherCreator, otherAssignee, WorkOrderStatus.Assigned)
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.AddRange(creator, otherCreator, assignee, otherAssignee);
        context.Set<WorkOrder>().AddRange(workOrders);
        await context.SaveChangesAsync();
        return new FilterSeed(creator.UserName, assignee.UserName, numbers);
    }

    private sealed record FilterSeed(
        string CreatorUserName,
        string AssigneeUserName,
        IReadOnlyDictionary<string, string> Numbers)
    {
        public string this[string key] => Numbers[key];

        public string[] AllNumbers => Numbers.Values.ToArray();
    }

    private static WorkOrder CreateWorkOrder(
        string number,
        Employee creator,
        Employee? assignee,
        WorkOrderStatus status) =>
        new()
        {
            Number = number,
            Title = number,
            Description = "Filter test",
            Instructions = "Summary must not expose this",
            Creator = creator,
            Assignee = assignee,
            Status = status
        };

    private static string[] ExtractNumbers(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray()
            .Select(item => item.GetProperty("Number").GetString()!)
            .ToArray();
    }

    private static string?[] ParameterNames(string methodName)
    {
        var method = typeof(WorkOrderTools).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                     ?? throw new InvalidOperationException($"Method '{methodName}' was not found.");

        return method.GetParameters()
            .Select(parameter => parameter.Name)
            .ToArray();
    }
}

internal static class WorkOrderNumberAssertions
{
    public static void ShouldBeSet(this IEnumerable<string> actual, params string[] expected)
    {
        actual.OrderBy(number => number).ShouldBe(expected.OrderBy(number => number));
    }
}
