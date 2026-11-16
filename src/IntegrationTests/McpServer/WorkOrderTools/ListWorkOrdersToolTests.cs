using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests;
using Shouldly;
using ProductionWorkOrderTools = ClearMeasure.Bootcamp.McpServer.Tools.WorkOrderTools;
using TestWorkOrderBuilder = ClearMeasure.Bootcamp.UnitTests.TestSupport.WorkOrderBuilder;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer.WorkOrderTools;

[TestFixture]
public class ListWorkOrdersToolTests
{
    [Test]
    public async Task ShouldListAllWorkOrders()
    {
        var employee = ObjectMother.Employee(TestData.UserName("user"), "John", "Doe");
        var number1 = TestData.Number("W");
        var number2 = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            employee,
            TestWorkOrderBuilder.Create().WithNumber(number1).WithTitle("Fix sink").CreatedBy(employee).Build(),
            TestWorkOrderBuilder.Create().WithNumber(number2).WithTitle("Paint wall").CreatedBy(employee).Build());

        var result = await ProductionWorkOrderTools.ListWorkOrders(McpWorkOrderSteps.Bus());

        result.ShouldContain(number1);
        result.ShouldContain(number2);
        result.ShouldContain("Fix sink");
        result.ShouldContain("Paint wall");
    }

    [Test]
    public async Task ShouldFilterWorkOrdersByStatus()
    {
        var employee = ObjectMother.Employee(TestData.UserName("user"), "John", "Doe");
        var number1 = TestData.Number("W");
        var number2 = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            employee,
            TestWorkOrderBuilder.Create().WithNumber(number1).WithTitle("Draft order").CreatedBy(employee)
                .WithStatus(WorkOrderStatus.Draft).Build(),
            TestWorkOrderBuilder.Create().WithNumber(number2).WithTitle("Assigned order").CreatedBy(employee)
                .WithStatus(WorkOrderStatus.Assigned).Build());

        var result = await ProductionWorkOrderTools.ListWorkOrders(McpWorkOrderSteps.Bus(), "Assigned");

        result.ShouldContain(number2);
        result.ShouldNotContain(number1);
    }

    [Test]
    public async Task ShouldFilterWorkOrdersByCreatorUsername()
    {
        var seed = await McpWorkOrderSteps.SeedListFiltersAsync();

        var result = await ProductionWorkOrderTools.ListWorkOrders(
            McpWorkOrderSteps.Bus(),
            creatorUsername: seed.CreatorUserName);

        McpWorkOrderSteps.ExtractNumbers(result).ShouldBeSet(
            seed["LJDRAFT"],
            seed["LJWA"],
            seed["LJWIP"],
            seed["LJCOMP"]);
    }

    [Test]
    public async Task ShouldFilterWorkOrdersByAssigneeUsername()
    {
        var seed = await McpWorkOrderSteps.SeedListFiltersAsync();

        var result = await ProductionWorkOrderTools.ListWorkOrders(
            McpWorkOrderSteps.Bus(),
            assigneeUsername: seed.AssigneeUserName);

        McpWorkOrderSteps.ExtractNumbers(result).ShouldBeSet(
            seed["LJWA"],
            seed["LJWIP"],
            seed["OTWIP"]);
    }

    [Test]
    public async Task ShouldAndAssigneeAndStatusFilters()
    {
        var seed = await McpWorkOrderSteps.SeedListFiltersAsync();

        var result = await ProductionWorkOrderTools.ListWorkOrders(
            McpWorkOrderSteps.Bus(),
            status: "InProgress",
            assigneeUsername: seed.AssigneeUserName);

        McpWorkOrderSteps.ExtractNumbers(result).ShouldBeSet(
            seed["LJWIP"],
            seed["OTWIP"]);
    }

    [Test]
    public async Task ShouldAndCreatorAndStatusFiltersForLovejoyDrafts()
    {
        var seed = await McpWorkOrderSteps.SeedListFiltersAsync();

        var result = await ProductionWorkOrderTools.ListWorkOrders(
            McpWorkOrderSteps.Bus(),
            status: "Draft",
            creatorUsername: seed.CreatorUserName);

        McpWorkOrderSteps.ExtractNumbers(result).ShouldBeSet(seed["LJDRAFT"]);
    }

    [Test]
    public async Task ShouldAndCreatorAssigneeAndStatusFilters()
    {
        var seed = await McpWorkOrderSteps.SeedListFiltersAsync();

        var result = await ProductionWorkOrderTools.ListWorkOrders(
            McpWorkOrderSteps.Bus(),
            status: "InProgress",
            creatorUsername: seed.CreatorUserName,
            assigneeUsername: seed.AssigneeUserName);

        McpWorkOrderSteps.ExtractNumbers(result).ShouldBeSet(seed["LJWIP"]);
    }

    [Test]
    public async Task ShouldTreatOmittedEmptyAndWhitespaceFiltersAsDisabled()
    {
        var seed = await McpWorkOrderSteps.SeedListFiltersAsync();
        var bus = McpWorkOrderSteps.Bus();

        var omitted = McpWorkOrderSteps.ExtractNumbers(await ProductionWorkOrderTools.ListWorkOrders(bus));
        var blank = McpWorkOrderSteps.ExtractNumbers(
            await ProductionWorkOrderTools.ListWorkOrders(bus, " ", string.Empty, "   "));

        omitted.Where(seed.AllNumbers.Contains).ShouldBeSet(seed.AllNumbers);
        blank.ShouldBeSet(omitted);
    }

    [Test]
    public async Task ShouldReturnEmptyForUnknownCreatorOrAssigneeWithoutCreatingEmployee()
    {
        var seed = await McpWorkOrderSteps.SeedListFiltersAsync();
        var bus = McpWorkOrderSteps.Bus();
        var employeeCount = await McpWorkOrderSteps.EmployeeCountAsync();

        var unknownCreator = await ProductionWorkOrderTools.ListWorkOrders(
            bus,
            status: "Assigned",
            creatorUsername: TestData.UserName("not-a-person"));
        var unknownAssignee = await ProductionWorkOrderTools.ListWorkOrders(
            bus,
            status: "Draft",
            creatorUsername: seed.CreatorUserName,
            assigneeUsername: TestData.UserName("not-a-person"));

        unknownCreator.ShouldBe("[]");
        unknownAssignee.ShouldBe("[]");
        (await McpWorkOrderSteps.EmployeeCountAsync()).ShouldBe(employeeCount);
    }

    [Test]
    public async Task ShouldPreserveInvalidStatusFailure()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => ProductionWorkOrderTools.ListWorkOrders(McpWorkOrderSteps.Bus(), status: "NotAStatus"));
    }

    [Test]
    public async Task ShouldKeepListWorkOrderSummaryWithoutInstructions()
    {
        var employee = ObjectMother.Employee(TestData.UserName("user"), "John", "Doe");
        var number = TestData.Number("W");
        var order = TestWorkOrderBuilder.Create()
            .WithNumber(number)
            .WithTitle("Summary shape")
            .CreatedBy(employee)
            .Build();
        order.Instructions = "detail only";
        await McpWorkOrderSteps.PersistAsync(employee, order);

        var result = await ProductionWorkOrderTools.ListWorkOrders(
            McpWorkOrderSteps.Bus(),
            creatorUsername: employee.UserName);

        McpWorkOrderSteps.ListItemHasProperty(result, "Instructions").ShouldBeFalse();
    }

    [Test]
    public async Task ShouldIncludePriorityAndBuildingInListOutput()
    {
        var creator = ObjectMother.Employee(TestData.UserName("creator"), "Jane", "Smith");
        var number = TestData.Number("W");
        await McpWorkOrderSteps.PersistAsync(
            creator,
            TestWorkOrderBuilder.Create()
                .WithNumber(number)
                .WithTitle("List output test")
                .CreatedBy(creator)
                .WithPriority(WorkOrderPriority.High)
                .InBuilding("East Wing")
                .Build());

        var result = await ProductionWorkOrderTools.ListWorkOrders(McpWorkOrderSteps.Bus());

        McpWorkOrderSteps.ReadListItemString(result, number, "Priority").ShouldBe("High");
        McpWorkOrderSteps.ReadListItemString(result, number, "Building").ShouldBe("East Wing");
    }
}
