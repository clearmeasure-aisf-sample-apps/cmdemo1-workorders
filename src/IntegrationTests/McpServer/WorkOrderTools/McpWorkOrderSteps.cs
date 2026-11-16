using System.Reflection;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.UnitTests;
using Microsoft.EntityFrameworkCore;
using ProductionWorkOrderTools = ClearMeasure.Bootcamp.McpServer.Tools.WorkOrderTools;
using TestWorkOrderBuilder = ClearMeasure.Bootcamp.UnitTests.TestSupport.WorkOrderBuilder;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer.WorkOrderTools;

/// <summary>
/// Shared arrange and MCP answer-reading steps for work-order tool integration tests. No assertions.
/// </summary>
public static class McpWorkOrderSteps
{
    /// <summary>Resolves the test <see cref="IBus"/>.</summary>
    public static IBus Bus() => TestHost.GetRequiredService<IBus>();

    /// <summary>A fresh work-order number generator for create-tool calls.</summary>
    public static WorkOrderNumberGenerator NumberGenerator() => new();

    /// <summary>Persists a Jane Smith creator under <paramref name="userName"/>.</summary>
    public static async Task SeedCreatorAsync(string userName)
    {
        await PersistAsync(ObjectMother.Employee(userName, "Jane", "Smith"));
    }

    /// <summary>Adds entities and saves in one DbContext scope.</summary>
    public static async Task PersistAsync(params object[] entities)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }

    /// <summary>Reads the <c>Number</c> property from a create/update/get JSON answer.</summary>
    public static string? ExtractWorkOrderNumber(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("Number").GetString();
    }

    /// <summary>Reads every <c>Number</c> from a list-work-orders JSON array.</summary>
    public static string[] ExtractNumbers(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray()
            .Select(item => item.GetProperty("Number").GetString()!)
            .ToArray();
    }

    /// <summary>Reads a string property from a JSON object answer.</summary>
    public static string? ReadString(string json, string propertyName)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty(propertyName).GetString();
    }

    /// <summary>Reads the JSON value kind of a property on an object answer.</summary>
    public static JsonValueKind ReadKind(string json, string propertyName)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty(propertyName).ValueKind;
    }

    /// <summary>Reads a string property from the list item whose number matches.</summary>
    public static string? ReadListItemString(string json, string number, string propertyName)
    {
        using var document = JsonDocument.Parse(json);
        var item = document.RootElement.EnumerateArray()
            .First(element => element.GetProperty("Number").GetString() == number);
        return item.GetProperty(propertyName).GetString();
    }

    /// <summary>Whether a list summary item exposes a named property.</summary>
    public static bool ListItemHasProperty(string json, string propertyName)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray().Single().TryGetProperty(propertyName, out _);
    }

    /// <summary>Loads a work order by number through the bus query.</summary>
    public static Task<WorkOrder?> LoadByNumberAsync(string number) =>
        Bus().Send(new WorkOrderByNumberQuery(number));

    /// <summary>Loads a work order row by number from a fresh DbContext.</summary>
    public static async Task<WorkOrder> RequirePersistedAsync(string number)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        return await context.Set<WorkOrder>().SingleAsync(order => order.Number == number);
    }

    /// <summary>Loads the single status-change row for a work order.</summary>
    public static async Task<WorkOrderStatusChange> RequireStatusChangeAsync(Guid workOrderId)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        return context.Set<WorkOrderStatusChange>().Single(row => row.WorkOrderId == workOrderId);
    }

    /// <summary>Counts employees currently stored.</summary>
    public static async Task<int> EmployeeCountAsync()
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        return await context.Set<Employee>().CountAsync();
    }

    /// <summary>Public parameter names of a static MCP work-order tool method.</summary>
    public static string?[] ParameterNames(string methodName)
    {
        var method = typeof(ProductionWorkOrderTools).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                     ?? throw new InvalidOperationException($"Method '{methodName}' was not found.");
        return method.GetParameters().Select(parameter => parameter.Name).ToArray();
    }

    /// <summary>
    /// Seeds two creators and two assignees with seven work orders keyed by filter role
    /// (for example <c>LJWA</c>: creator's order assigned to the assignee).
    /// </summary>
    public static async Task<ListWorkOrderFilterSeed> SeedListFiltersAsync()
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Filter", "Creator");
        var otherCreator = ObjectMother.Employee(TestData.UserName("other-creator"), "Other", "Creator");
        var assignee = ObjectMother.Employee(TestData.UserName("assignee"), "Filter", "Assignee");
        var otherAssignee = ObjectMother.Employee(TestData.UserName("other-assignee"), "Other", "Assignee");
        var numbers = new[] { "LJDRAFT", "OTDRAFT", "LJWA", "LJWIP", "OTWIP", "LJCOMP", "OTASN" }
            .ToDictionary(key => key, _ => TestData.Number("F"));
        var workOrders = new[]
        {
            FilterOrder(numbers["LJDRAFT"], creator, null, WorkOrderStatus.Draft),
            FilterOrder(numbers["OTDRAFT"], otherCreator, null, WorkOrderStatus.Draft),
            FilterOrder(numbers["LJWA"], creator, assignee, WorkOrderStatus.Assigned),
            FilterOrder(numbers["LJWIP"], creator, assignee, WorkOrderStatus.InProgress),
            FilterOrder(numbers["OTWIP"], otherCreator, assignee, WorkOrderStatus.InProgress),
            FilterOrder(numbers["LJCOMP"], creator, otherAssignee, WorkOrderStatus.Complete),
            FilterOrder(numbers["OTASN"], otherCreator, otherAssignee, WorkOrderStatus.Assigned)
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.AddRange(creator, otherCreator, assignee, otherAssignee);
        context.Set<WorkOrder>().AddRange(workOrders);
        await context.SaveChangesAsync();
        return new ListWorkOrderFilterSeed(creator.UserName, assignee.UserName, numbers);
    }

    /// <summary>Persists a Jane Smith draft configured by <paramref name="arrange"/>.</summary>
    public static async Task<(Employee Creator, string Number)> PersistDraftAsync(DraftArrange arrange)
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Jane", "Smith");
        var number = TestData.Number("W");
        await PersistAsync(
            creator,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle(arrange.Title)
                .CreatedBy(creator)
                .WithPriority(arrange.Priority)
                .InBuilding(arrange.Building)
                .InRoom(arrange.Room)
                .DueOn(arrange.DueDate)
                .Build());
        return (creator, number);
    }

    /// <summary>Creates a draft through the MCP create tool and returns its number.</summary>
    public static async Task<(string CreatorUserName, string Number)> CreateDraftViaToolAsync(McpDraftCreate create)
    {
        var creatorUserName = TestData.UserName("creator");
        await SeedCreatorAsync(creatorUserName);
        var createResult = await ProductionWorkOrderTools.CreateWorkOrder(
            Bus(),
            NumberGenerator(),
            create.Title,
            create.Description,
            creatorUserName,
            roomNumber: create.RoomNumber,
            dueDate: create.DueDate,
            instructions: create.Instructions);
        return (creatorUserName, ExtractWorkOrderNumber(createResult)!);
    }

    /// <summary>Persists an assigned work order with the classic field values used by save refusal tests.</summary>
    public static async Task<(Employee Creator, string Number)> PersistAssignedSaveFixtureAsync()
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Jane", "Smith");
        var number = TestData.Number("W");
        var order = TestWorkOrderBuilder.Create()
            .WithNumber(number)
            .WithTitle("Assigned title")
            .WithDescription("Assigned description")
            .InRoom("Room 1")
            .DueOn(new DateOnly(2026, 9, 3))
            .CreatedBy(creator)
            .WithStatus(WorkOrderStatus.Assigned)
            .Build();
        order.Instructions = "Assigned instructions";
        await PersistAsync(creator, order);
        return (creator, number);
    }

    /// <summary>Persists a draft owned by Jane with an Other User also stored for non-creator checks.</summary>
    public static async Task<(Employee Creator, Employee Other, string Number)> PersistDraftWithOtherUserAsync(
        DraftArrange arrange)
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Jane", "Smith");
        var otherUser = ObjectMother.Employee(TestData.UserName("other"), "Other", "User");
        var number = TestData.Number("W");
        await PersistAsync(
            creator,
            otherUser,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle(arrange.Title)
                .CreatedBy(creator)
                .WithPriority(arrange.Priority)
                .InBuilding(arrange.Building)
                .InRoom(arrange.Room)
                .DueOn(arrange.DueDate)
                .Build());
        return (creator, otherUser, number);
    }

    private static WorkOrder FilterOrder(
        string number,
        Employee creator,
        Employee? assignee,
        WorkOrderStatus status)
    {
        var order = TestWorkOrderBuilder.Create()
            .WithNumber(number)
            .WithTitle(number)
            .WithDescription("Filter test")
            .CreatedBy(creator)
            .AssignedTo(assignee)
            .WithStatus(status)
            .Build();
        order.Instructions = "Summary must not expose this";
        return order;
    }
}
