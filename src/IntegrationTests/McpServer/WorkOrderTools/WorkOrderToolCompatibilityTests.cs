using System.Reflection;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Shouldly;
using ProductionWorkOrderTools = ClearMeasure.Bootcamp.McpServer.Tools.WorkOrderTools;
using TestWorkOrderBuilder = ClearMeasure.Bootcamp.UnitTests.TestSupport.WorkOrderBuilder;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer.WorkOrderTools;

[TestFixture]
public class WorkOrderToolCompatibilityTests
{
    [Test]
    public void ShouldKeepWorkOrderToolArgumentListsBackwardCompatible()
    {
        McpWorkOrderSteps.ParameterNames(nameof(ProductionWorkOrderTools.CreateWorkOrder)).ShouldBe(
            ["bus", "numberGenerator", "title", "description", "creatorUsername", "roomNumber", "dueDate", "instructions", "priority", "building"]);
        McpWorkOrderSteps.ParameterNames(nameof(ProductionWorkOrderTools.SaveWorkOrder)).ShouldBe(
            ["bus", "workOrderNumber", "executingUsername", "title", "description", "instructions", "roomNumber", "dueDate"]);
        McpWorkOrderSteps.ParameterNames(nameof(ProductionWorkOrderTools.CreateDatedWorkOrders)).ShouldBe(
            ["bus", "timeProvider", "creatorUsername", "assigneeUsername", "title", "description", "dueDates", "saturdayCount"]);
    }

    [Test]
    public void ShouldNotExposeSaveOnExecuteWorkOrderCommand()
    {
        var field = typeof(WorkOrderCommandExecutor)
            .GetField("CommandFactories", BindingFlags.NonPublic | BindingFlags.Static);
        field.ShouldNotBeNull();
        var factories = (IReadOnlyDictionary<string, Func<WorkOrder, Employee, StateCommandBase>>)field.GetValue(null)!;

        factories.Keys.ShouldNotContain("Save");
        factories.Keys.ShouldNotContain("SaveDraftCommand");
        factories.Keys.ShouldNotContain(SaveDraftCommand.Name);
    }
}
