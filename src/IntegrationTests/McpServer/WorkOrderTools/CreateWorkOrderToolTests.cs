using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests;
using Shouldly;
using ProductionWorkOrderTools = ClearMeasure.Bootcamp.McpServer.Tools.WorkOrderTools;
using TestWorkOrderBuilder = ClearMeasure.Bootcamp.UnitTests.TestSupport.WorkOrderBuilder;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer.WorkOrderTools;

[TestFixture]
public class CreateWorkOrderToolTests
{
    [Test]
    public async Task ShouldCreateDraftWorkOrder()
    {
        var creatorUserName = TestData.UserName("creator");
        await McpWorkOrderSteps.SeedCreatorAsync(creatorUserName);

        var result = await ProductionWorkOrderTools.CreateWorkOrder(
            McpWorkOrderSteps.Bus(),
            McpWorkOrderSteps.NumberGenerator(),
            "New Work Order",
            "Fix the broken window",
            creatorUserName);

        result.ShouldContain("New Work Order");
        result.ShouldContain("Fix the broken window");
        result.ShouldContain("Draft");
    }

    [Test]
    public async Task ShouldReturnErrorForMissingCreator()
    {
        var missingUserName = TestData.UserName("missing");

        var result = await ProductionWorkOrderTools.CreateWorkOrder(
            McpWorkOrderSteps.Bus(),
            McpWorkOrderSteps.NumberGenerator(),
            "Title",
            "Description",
            missingUserName);

        result.ShouldContain("not found");
    }

    [Test]
    public async Task ShouldCreateWithHighPriorityAndBuilding()
    {
        var creatorUserName = TestData.UserName("creator");
        await McpWorkOrderSteps.SeedCreatorAsync(creatorUserName);
        var bus = McpWorkOrderSteps.Bus();

        var createResult = await ProductionWorkOrderTools.CreateWorkOrder(
            bus,
            McpWorkOrderSteps.NumberGenerator(),
            "High priority task",
            "Description",
            creatorUserName,
            priority: "High",
            building: "Sanctuary");

        McpWorkOrderSteps.ReadString(createResult, "Priority").ShouldBe("High");
        McpWorkOrderSteps.ReadString(createResult, "Building").ShouldBe("Sanctuary");
        var number = McpWorkOrderSteps.ExtractWorkOrderNumber(createResult)!;
        var detail = await ProductionWorkOrderTools.GetWorkOrder(bus, number);
        McpWorkOrderSteps.ReadString(detail, "Priority").ShouldBe("High");
        McpWorkOrderSteps.ReadString(detail, "Building").ShouldBe("Sanctuary");
    }

    [Test]
    public async Task ShouldCreateWithDefaultMediumPriorityAndNullBuilding()
    {
        var creatorUserName = TestData.UserName("creator");
        await McpWorkOrderSteps.SeedCreatorAsync(creatorUserName);

        var createResult = await ProductionWorkOrderTools.CreateWorkOrder(
            McpWorkOrderSteps.Bus(),
            McpWorkOrderSteps.NumberGenerator(),
            "Normal task",
            "Description",
            creatorUserName);

        McpWorkOrderSteps.ReadString(createResult, "Priority").ShouldBe("Medium");
        McpWorkOrderSteps.ReadKind(createResult, "Building").ShouldBe(JsonValueKind.Null);
    }

    [Test]
    public async Task ShouldRejectDescriptionLongerThan4000CharactersWhenCreatingWorkOrder()
    {
        var creatorUserName = TestData.UserName("creator");
        await McpWorkOrderSteps.SeedCreatorAsync(creatorUserName);
        var tooLongDescription = new string('d', WorkOrder.DescriptionMaxLength + 1);

        var result = await ProductionWorkOrderTools.CreateWorkOrder(
            McpWorkOrderSteps.Bus(),
            McpWorkOrderSteps.NumberGenerator(),
            "Title",
            tooLongDescription,
            creatorUserName);

        result.ShouldBe("Description cannot exceed 4000 characters.");
    }
}
