using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.IntegrationTests.McpServer.WorkOrderTools;
using Shouldly;
using ProductionWorkOrderTools = ClearMeasure.Bootcamp.McpServer.Tools.WorkOrderTools;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpCreateWorkOrderInstructionsTests
{
    [Test]
    public async Task ShouldPersistExplicitInstructionsDistinctFromDescription()
    {
        var creatorUserName = TestData.UserName("creator");
        await McpWorkOrderSteps.SeedCreatorAsync(creatorUserName);
        const string description = "Fix the broken window in the fellowship hall";
        const string instructions = "Bring ladder from east shed; do not block wheelchair ramp";

        var result = await ProductionWorkOrderTools.CreateWorkOrder(
            McpWorkOrderSteps.Bus(),
            McpWorkOrderSteps.NumberGenerator(),
            "New Work Order",
            description,
            creatorUserName,
            instructions: instructions);

        result.ShouldContain("New Work Order");
        var number = McpWorkOrderSteps.ExtractWorkOrderNumber(result);
        number.ShouldNotBeNullOrWhiteSpace();
        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number!);
        reloaded.ShouldNotBeNull();
        reloaded.Instructions.ShouldBe(instructions);
        reloaded.Instructions.ShouldNotBe(description);
        reloaded.Description.ShouldBe(description);
    }

    [Test]
    public async Task ShouldPersistEmptyInstructionsWhenOmitted()
    {
        var creatorUserName = TestData.UserName("creator");
        await McpWorkOrderSteps.SeedCreatorAsync(creatorUserName);

        var result = await ProductionWorkOrderTools.CreateWorkOrder(
            McpWorkOrderSteps.Bus(),
            McpWorkOrderSteps.NumberGenerator(),
            "Omitted instructions",
            "Description only",
            creatorUserName);

        var number = McpWorkOrderSteps.ExtractWorkOrderNumber(result);
        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number!);
        reloaded!.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldPersistEmptyInstructionsWhenEmpty()
    {
        var creatorUserName = TestData.UserName("creator");
        await McpWorkOrderSteps.SeedCreatorAsync(creatorUserName);

        var result = await ProductionWorkOrderTools.CreateWorkOrder(
            McpWorkOrderSteps.Bus(),
            McpWorkOrderSteps.NumberGenerator(),
            "Empty instructions",
            "Description only",
            creatorUserName,
            instructions: string.Empty);

        var number = McpWorkOrderSteps.ExtractWorkOrderNumber(result);
        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number!);
        reloaded!.Instructions.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldTruncateInstructionsTo4000CharactersOnCreate()
    {
        var creatorUserName = TestData.UserName("creator");
        await McpWorkOrderSteps.SeedCreatorAsync(creatorUserName);
        var longInstructions = new string('I', WorkOrder.InstructionsMaxLength + 1);
        var expected = new string('I', WorkOrder.InstructionsMaxLength);

        var result = await ProductionWorkOrderTools.CreateWorkOrder(
            McpWorkOrderSteps.Bus(),
            McpWorkOrderSteps.NumberGenerator(),
            "Long instructions",
            "Description",
            creatorUserName,
            instructions: longInstructions);

        result.ShouldContain("Long instructions");
        var number = McpWorkOrderSteps.ExtractWorkOrderNumber(result);
        var reloaded = await McpWorkOrderSteps.LoadByNumberAsync(number!);
        reloaded!.Instructions!.Length.ShouldBe(WorkOrder.InstructionsMaxLength);
        reloaded.Instructions.ShouldBe(expected);
    }

    [Test]
    public async Task ShouldReturnNotFoundWhenCreatorMissing()
    {
        var missingUserName = TestData.UserName("missing");

        var result = await ProductionWorkOrderTools.CreateWorkOrder(
            McpWorkOrderSteps.Bus(),
            McpWorkOrderSteps.NumberGenerator(),
            "Title",
            "Description",
            missingUserName,
            instructions: "Some instructions");

        result.ShouldBe($"Employee with username '{missingUserName}' not found.");
    }
}
