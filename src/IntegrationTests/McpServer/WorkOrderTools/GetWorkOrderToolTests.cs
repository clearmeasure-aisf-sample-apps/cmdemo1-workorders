using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests;
using Shouldly;
using ProductionWorkOrderTools = ClearMeasure.Bootcamp.McpServer.Tools.WorkOrderTools;
using TestWorkOrderBuilder = ClearMeasure.Bootcamp.UnitTests.TestSupport.WorkOrderBuilder;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer.WorkOrderTools;

[TestFixture]
public class GetWorkOrderToolTests
{
    [Test]
    public async Task ShouldGetWorkOrderCoreFields()
    {
        var employee = ObjectMother.Employee(TestData.UserName("user"), "John", "Doe");
        var number = TestData.Number("W");
        var order = TestWorkOrderBuilder.Create()
            .WithNumber(number)
            .WithTitle("Test order")
            .WithDescription("A description")
            .InRoom("101")
            .DueOn(new DateOnly(2026, 9, 12))
            .CreatedBy(employee)
            .WithStatus(WorkOrderStatus.Draft)
            .Build();
        order.Instructions = "preschool quiet";
        await McpWorkOrderSteps.PersistAsync(employee, order);

        var result = await ProductionWorkOrderTools.GetWorkOrder(McpWorkOrderSteps.Bus(), number);

        McpWorkOrderSteps.ReadString(result, "Number").ShouldBe(number);
        McpWorkOrderSteps.ReadString(result, "Title").ShouldBe("Test order");
        McpWorkOrderSteps.ReadString(result, "Description").ShouldBe("A description");
        McpWorkOrderSteps.ReadString(result, "Instructions").ShouldBe("preschool quiet");
        McpWorkOrderSteps.ReadString(result, "RoomNumber").ShouldBe("101");
        McpWorkOrderSteps.ReadString(result, "DueDate").ShouldBe("2026-09-12");
        McpWorkOrderSteps.ReadString(result, "Status").ShouldBe("Draft");
    }

    [Test]
    public async Task ShouldGetWorkOrderPeopleDatesPriorityAndBuilding()
    {
        var employee = ObjectMother.Employee(TestData.UserName("user"), "John", "Doe");
        var number = TestData.Number("W");
        var createdDate = new DateTime(2026, 8, 25, 14, 30, 0, DateTimeKind.Utc);
        await McpWorkOrderSteps.PersistAsync(
            employee,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("Test order")
                .CreatedBy(employee)
                .CreatedAt(createdDate)
                .WithPriority(WorkOrderPriority.High)
                .InBuilding("Main Hall")
                .Build());

        var result = await ProductionWorkOrderTools.GetWorkOrder(McpWorkOrderSteps.Bus(), number);

        McpWorkOrderSteps.ReadString(result, "Creator").ShouldBe("John Doe");
        McpWorkOrderSteps.ReadString(result, "CreatorUsername").ShouldBe(employee.UserName);
        McpWorkOrderSteps.ReadKind(result, "Assignee").ShouldBe(JsonValueKind.Null);
        McpWorkOrderSteps.ReadKind(result, "AssigneeUsername").ShouldBe(JsonValueKind.Null);
        using var document = JsonDocument.Parse(result);
        document.RootElement.GetProperty("CreatedDate").GetDateTime().ShouldBe(createdDate);
        McpWorkOrderSteps.ReadKind(result, "AssignedDate").ShouldBe(JsonValueKind.Null);
        McpWorkOrderSteps.ReadKind(result, "CompletedDate").ShouldBe(JsonValueKind.Null);
        McpWorkOrderSteps.ReadString(result, "Priority").ShouldBe("High");
        McpWorkOrderSteps.ReadString(result, "Building").ShouldBe("Main Hall");
    }

    [Test]
    public async Task ShouldGetWorkOrderWithEmptyInstructionsPropertyWhenInstructionsOmitted()
    {
        var employee = ObjectMother.Employee(TestData.UserName("user"), "John", "Doe");
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            employee,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("No special instructions")
                .CreatedBy(employee)
                .Build());

        var result = await ProductionWorkOrderTools.GetWorkOrder(McpWorkOrderSteps.Bus(), number);

        McpWorkOrderSteps.ReadKind(result, "Instructions").ShouldBe(JsonValueKind.String);
        McpWorkOrderSteps.ReadString(result, "Instructions").ShouldBe(string.Empty);
        result.ShouldContain("\"Instructions\": \"\"");
        result.ShouldNotContain("\"Instructions\": \"null\"");
    }

    [Test]
    public async Task ShouldReturnNotFoundForMissingWorkOrder()
    {
        var result = await ProductionWorkOrderTools.GetWorkOrder(McpWorkOrderSteps.Bus(), "NONEXISTENT");

        result.ShouldBe("No work order found with number 'NONEXISTENT'.");
    }
}
