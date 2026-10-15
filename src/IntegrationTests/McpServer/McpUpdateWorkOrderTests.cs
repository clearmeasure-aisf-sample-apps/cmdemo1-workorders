using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.IntegrationTests.TestSupport;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpUpdateWorkOrderTests
{
    private string _creator = string.Empty;
    private string _other = string.Empty;
    private string _worker = string.Empty;
    private string _stem = string.Empty;

    [SetUp]
    public void Setup()
    {
        _creator = TestData.UserName("creator");
        _other = TestData.UserName("other");
        _worker = TestData.UserName("worker");
        _stem = TestData.NumberSeries(1)[0][..5];
    }

    [Test]
    public async Task ShouldCreateWithHighPriorityAndBuilding()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");

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
            _creator,
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
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");

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
            _creator);

        var result = JsonDocument.Parse(createResult);
        result.RootElement.GetProperty("Priority").GetString().ShouldBe("Medium");
        result.RootElement.GetProperty("Building").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Test]
    public async Task ShouldUpdatePriority()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}700",
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
            $"{_stem}700",
            _creator,
            priority: "High");

        var parsed = JsonDocument.Parse(result);
        parsed.RootElement.GetProperty("Priority").GetString().ShouldBe("High");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery($"{_stem}700"));
        reloaded!.Priority.ShouldBe(WorkOrderPriority.High);
    }

    [Test]
    public async Task ShouldUpdateBuildingAndRoom()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}701",
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
            $"{_stem}701",
            _creator,
            building: "New Building",
            roomNumber: "202");

        var parsed = JsonDocument.Parse(result);
        parsed.RootElement.GetProperty("Building").GetString().ShouldBe("New Building");
        parsed.RootElement.GetProperty("RoomNumber").GetString().ShouldBe("202");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery($"{_stem}701"));
        reloaded!.Building.ShouldBe("New Building");
        reloaded.RoomNumber.ShouldBe("202");
    }

    [Test]
    public async Task ShouldUpdateDueDate()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}702",
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
            $"{_stem}702",
            _creator,
            dueDate: newDueDate.ToString("yyyy-MM-dd"));

        var parsed = JsonDocument.Parse(result);
        parsed.RootElement.GetProperty("DueDate").GetString().ShouldBe("2026-12-25");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery($"{_stem}702"));
        reloaded!.DueDate.ShouldBe(newDueDate);
    }

    [Test]
    public async Task ShouldLeaveOmittedFieldsUnchanged()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var originalDueDate = new DateOnly(2026, 9, 1);
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}703",
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
            $"{_stem}703",
            _creator,
            roomNumber: "301");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery($"{_stem}703"));
        reloaded!.Priority.ShouldBe(WorkOrderPriority.High);
        reloaded.Building.ShouldBe("West Wing");
        reloaded.RoomNumber.ShouldBe("301");
        reloaded.DueDate.ShouldBe(originalDueDate);
    }

    [Test]
    public async Task ShouldClearEmptyBuilding()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}704",
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
            $"{_stem}704",
            _creator,
            building: string.Empty);

        var reloaded = await bus.Send(new WorkOrderByNumberQuery($"{_stem}704"));
        reloaded!.Building.ShouldBeNull();
    }

    [Test]
    public async Task ShouldClearEmptyRoom()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}705",
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
            $"{_stem}705",
            _creator,
            roomNumber: "   ");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery($"{_stem}705"));
        reloaded!.RoomNumber.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldClearEmptyDueDate()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}706",
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
            $"{_stem}706",
            _creator,
            dueDate: string.Empty);

        var reloaded = await bus.Send(new WorkOrderByNumberQuery($"{_stem}706"));
        reloaded!.DueDate.ShouldBeNull();
    }

    [Test]
    public async Task ShouldReturnErrorForInvalidPriority()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}710",
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
            $"{_stem}710",
            _creator,
            priority: "Urgent");

        result.ShouldContain("Invalid priority");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery($"{_stem}710"));
        reloaded!.Priority.ShouldBe(WorkOrderPriority.Medium);
    }

    [Test]
    public async Task ShouldReturnErrorForBuildingExceedingMaxLength()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}711",
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
            $"{_stem}711",
            _creator,
            building: tooLong);

        result.ShouldContain("Building cannot exceed");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery($"{_stem}711"));
        reloaded!.Building.ShouldBe("Original");
    }

    [Test]
    public async Task ShouldReturnErrorForMalformedDueDate()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}712",
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
            $"{_stem}712",
            _creator,
            dueDate: "not-a-date");

        result.ShouldContain("Invalid due date");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery($"{_stem}712"));
        reloaded!.DueDate.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Test]
    public async Task ShouldReturnErrorForUnknownWorkOrder()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(creator);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.UpdateWorkOrder(
            bus,
            "UNKNOWN-WO",
            _creator,
            priority: "Low");

        result.ShouldBe("No work order found with number 'UNKNOWN-WO'.");
    }

    [Test]
    public async Task ShouldReturnErrorForUnknownUser()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}713",
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
            $"{_stem}713",
            "unknown-user",
            priority: "Low");

        result.ShouldBe("Employee with username 'unknown-user' not found.");
    }

    [Test]
    public async Task ShouldReturnErrorForNonDraftWorkOrder()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var assignee = new Employee(_worker, "Sam", "Worker", "worker@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = $"{_stem}714",
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
            $"{_stem}714",
            _creator,
            priority: "High");

        result.ShouldContain("cannot be executed");
        result.ShouldContain("Assigned");
    }

    [Test]
    public async Task ShouldReturnErrorForNonCreator()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var otherUser = new Employee(_other, "Other", "User", "other@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}715",
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
            $"{_stem}715",
            _other,
            priority: "High");

        result.ShouldContain("cannot be executed");
    }

    [Test]
    public async Task ShouldNotChangeCreatedOrCompletedDates()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var createdDate = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var completedDate = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}716",
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
            $"{_stem}716",
            _creator,
            priority: "High");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery($"{_stem}716"));
        reloaded!.CreatedDate.ShouldBe(createdDate);
        reloaded.CompletedDate.ShouldBe(completedDate);
    }

    [Test]
    public async Task ShouldIncludePriorityAndBuildingInListOutput()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}720",
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
        var item = document.RootElement.EnumerateArray()
            .First(e => e.GetProperty("Number").GetString() == $"{_stem}720");
        item.GetProperty("Priority").GetString().ShouldBe("High");
        item.GetProperty("Building").GetString().ShouldBe("East Wing");
    }

    [Test]
    public async Task ShouldReturnErrorForBlankPriority()
    {
        var creator = new Employee(_creator, "Jane", "Smith", "jane@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Number = $"{_stem}721",
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
            $"{_stem}721",
            _creator,
            priority: "   ");

        result.ShouldContain("Priority cannot be cleared");

        var reloaded = await bus.Send(new WorkOrderByNumberQuery($"{_stem}721"));
        reloaded!.Priority.ShouldBe(WorkOrderPriority.High);
    }
}
