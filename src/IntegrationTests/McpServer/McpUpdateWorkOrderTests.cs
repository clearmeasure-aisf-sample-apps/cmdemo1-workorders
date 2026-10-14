using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpUpdateWorkOrderTests
{
    [SetUp]
    public void Setup()
    {
        new DatabaseTests().Clean();
    }

    [Test]
    public async Task ShouldCreateWithHighPriorityAndBuilding()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "High priority task",
            "Description",
            "creator1",
            priority: "High",
            building: "Sanctuary");

        var result = JsonDocument.Parse(createResult);
        result.RootElement.GetProperty("Priority").GetString().ShouldBe("High");
        result.RootElement.GetProperty("Building").GetString().ShouldBe("Sanctuary");

        var number = result.RootElement.GetProperty("Number").GetString()!;
        var getResult = await WorkOrderTools.GetWorkOrder(bus, number);
        var detail = JsonDocument.Parse(getResult);
        detail.RootElement.GetProperty("Priority").GetString().ShouldBe("High");
        detail.RootElement.GetProperty("Building").GetString().ShouldBe("Sanctuary");
    }

    [Test]
    public async Task ShouldCreateWithDefaultMediumPriorityAndNullBuilding()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var numberGenerator = new WorkOrderNumberGenerator();
        var createResult = await WorkOrderTools.CreateWorkOrder(
            bus,
            numberGenerator,
            "Normal task",
            "Description",
            "creator1");

        var result = JsonDocument.Parse(createResult);
        result.RootElement.GetProperty("Priority").GetString().ShouldBe("Medium");
        result.RootElement.GetProperty("Building").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Test]
    public async Task ShouldUpdatePriority()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-700",
            Title = "Update priority",
            Status = WorkOrderStatus.Draft,
            Priority = WorkOrderPriority.Low
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-700",
            "creator1",
            priority: "High");

        var parsed = JsonDocument.Parse(result);
        parsed.RootElement.GetProperty("Priority").GetString().ShouldBe("High");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-700"));
        reloaded!.Priority.ShouldBe(WorkOrderPriority.High);
    }

    [Test]
    public async Task ShouldUpdateBuildingAndRoom()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-701",
            Title = "Update building",
            Status = WorkOrderStatus.Draft,
            Building = "Old Building",
            RoomNumber = "101"
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-701",
            "creator1",
            building: "New Building",
            roomNumber: "202");

        var parsed = JsonDocument.Parse(result);
        parsed.RootElement.GetProperty("Building").GetString().ShouldBe("New Building");
        parsed.RootElement.GetProperty("RoomNumber").GetString().ShouldBe("202");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-701"));
        reloaded!.Building.ShouldBe("New Building");
        reloaded.RoomNumber.ShouldBe("202");
    }

    [Test]
    public async Task ShouldUpdateDueDate()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-702",
            Title = "Update due date",
            Status = WorkOrderStatus.Draft,
            DueDate = new DateOnly(2026, 8, 1)
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var newDueDate = new DateOnly(2026, 12, 25);
        var result = await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-702",
            "creator1",
            dueDate: newDueDate.ToString("yyyy-MM-dd"));

        var parsed = JsonDocument.Parse(result);
        parsed.RootElement.GetProperty("DueDate").GetString().ShouldBe("2026-12-25");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-702"));
        reloaded!.DueDate.ShouldBe(newDueDate);
    }

    [Test]
    public async Task ShouldLeaveOmittedFieldsUnchanged()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var originalDueDate = new DateOnly(2026, 9, 1);
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-703",
            Title = "Omitted fields",
            Status = WorkOrderStatus.Draft,
            Priority = WorkOrderPriority.High,
            Building = "West Wing",
            RoomNumber = "300",
            DueDate = originalDueDate
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-703",
            "creator1",
            roomNumber: "301");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-703"));
        reloaded!.Priority.ShouldBe(WorkOrderPriority.High);
        reloaded.Building.ShouldBe("West Wing");
        reloaded.RoomNumber.ShouldBe("301");
        reloaded.DueDate.ShouldBe(originalDueDate);
    }

    [Test]
    public async Task ShouldClearEmptyBuilding()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-704",
            Title = "Clear building",
            Status = WorkOrderStatus.Draft,
            Building = "Main Hall"
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-704",
            "creator1",
            building: string.Empty);

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-704"));
        reloaded!.Building.ShouldBeNull();
    }

    [Test]
    public async Task ShouldClearEmptyRoom()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-705",
            Title = "Clear room",
            Status = WorkOrderStatus.Draft,
            RoomNumber = "501"
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-705",
            "creator1",
            roomNumber: "   ");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-705"));
        reloaded!.RoomNumber.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldClearEmptyDueDate()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-706",
            Title = "Clear due date",
            Status = WorkOrderStatus.Draft,
            DueDate = new DateOnly(2026, 10, 1)
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-706",
            "creator1",
            dueDate: string.Empty);

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-706"));
        reloaded!.DueDate.ShouldBeNull();
    }

    [Test]
    public async Task ShouldReturnErrorForInvalidPriority()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-710",
            Title = "Invalid priority test",
            Status = WorkOrderStatus.Draft,
            Priority = WorkOrderPriority.Medium
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-710",
            "creator1",
            priority: "Urgent");

        result.ShouldContain("Invalid priority");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-710"));
        reloaded!.Priority.ShouldBe(WorkOrderPriority.Medium);
    }

    [Test]
    public async Task ShouldReturnErrorForBuildingExceedingMaxLength()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-711",
            Title = "Building too long test",
            Status = WorkOrderStatus.Draft,
            Building = "Original"
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var tooLong = new string('x', WorkOrder.BuildingMaxLength + 1);
        var result = await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-711",
            "creator1",
            building: tooLong);

        result.ShouldContain("Building cannot exceed");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-711"));
        reloaded!.Building.ShouldBe("Original");
    }

    [Test]
    public async Task ShouldReturnErrorForMalformedDueDate()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-712",
            Title = "Bad date test",
            Status = WorkOrderStatus.Draft,
            DueDate = new DateOnly(2026, 8, 1)
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-712",
            "creator1",
            dueDate: "not-a-date");

        result.ShouldContain("Invalid due date");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-712"));
        reloaded!.DueDate.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Test]
    public async Task ShouldReturnErrorForUnknownWorkOrder()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.UpdateWorkOrder(
            bus,
            "UNKNOWN-WO",
            "creator1",
            priority: "Low");

        result.ShouldBe("No work order found with number 'UNKNOWN-WO'.");
    }

    [Test]
    public async Task ShouldReturnErrorForUnknownUser()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-713",
            Title = "Unknown user test",
            Status = WorkOrderStatus.Draft
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-713",
            "unknown-user",
            priority: "Low");

        result.ShouldBe("Employee with username 'unknown-user' not found.");
    }

    [Test]
    public async Task ShouldReturnErrorForNonDraftWorkOrder()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var assignee = new Employee("worker1", "Sam", "Worker", "worker@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = "WO-714",
            Title = "Assigned work order",
            Status = WorkOrderStatus.Assigned
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(assignee);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-714",
            "creator1",
            priority: "High");

        result.ShouldContain("cannot be executed");
        result.ShouldContain("Assigned");
    }

    [Test]
    public async Task ShouldReturnErrorForNonCreator()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var otherUser = new Employee("other1", "Other", "User", "other@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-715",
            Title = "Draft work order",
            Status = WorkOrderStatus.Draft
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(otherUser);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-715",
            "other1",
            priority: "High");

        result.ShouldContain("cannot be executed");
    }

    [Test]
    public async Task ShouldNotChangeCreatedOrCompletedDates()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var createdDate = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var completedDate = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-716",
            Title = "Dates test",
            Status = WorkOrderStatus.Draft,
            CreatedDate = createdDate,
            CompletedDate = completedDate
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-716",
            "creator1",
            priority: "High");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-716"));
        reloaded!.CreatedDate.ShouldBe(createdDate);
        reloaded.CompletedDate.ShouldBe(completedDate);
    }

    [Test]
    public async Task ShouldIncludePriorityAndBuildingInListOutput()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-720",
            Title = "List output test",
            Status = WorkOrderStatus.Draft,
            Priority = WorkOrderPriority.High,
            Building = "East Wing"
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ListWorkOrders(bus);

        using var document = JsonDocument.Parse(result);
        var item = document.RootElement.EnumerateArray().First();
        item.GetProperty("Priority").GetString().ShouldBe("High");
        item.GetProperty("Building").GetString().ShouldBe("East Wing");
    }

    [Test]
    public async Task ShouldReturnErrorForBlankPriority()
    {
        var creator = new Employee("creator1", "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = "WO-721",
            Title = "Blank priority test",
            Status = WorkOrderStatus.Draft,
            Priority = WorkOrderPriority.High
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        context.Add(workOrder);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.UpdateWorkOrder(
            bus,
            "WO-721",
            "creator1",
            priority: "   ");

        result.ShouldContain("Priority cannot be cleared");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery("WO-721"));
        reloaded!.Priority.ShouldBe(WorkOrderPriority.High);
    }
}
