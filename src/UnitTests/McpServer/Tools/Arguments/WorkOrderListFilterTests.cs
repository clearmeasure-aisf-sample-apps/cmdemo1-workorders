using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer.Tools.Arguments;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools.Arguments;

[TestFixture]
public class WorkOrderListFilterTests
{
    [Test]
    public async Task ShouldMatchStatusCreatorAndAssignee_WhenTheyAreNamed()
    {
        var creator = WorkOrderToolMother.Creator();
        var assignee = WorkOrderToolMother.OtherEmployee();
        var bus = new StubToolBus().With(creator).With(assignee);

        var query = await new WorkOrderListFilter("Assigned", "jsmith", "bwayne").ToQueryAsync(bus);

        query.ShouldNotBeNull();
        query.StatusKey.ShouldBe(WorkOrderStatus.Assigned.Key);
        query.Creator.ShouldBe(creator);
        query.Assignee.ShouldBe(assignee);
    }

    [Test]
    public async Task ShouldMatchEverything_WhenEveryFilterIsBlank()
    {
        var bus = new StubToolBus();

        var query = await new WorkOrderListFilter(" ", "", null).ToQueryAsync(bus);

        query.ShouldNotBeNull();
        query.StatusKey.ShouldBeNull();
        bus.SentRequests.ShouldBeEmpty();
    }

    [TestCase("nobody", null)]
    [TestCase(null, "nobody")]
    public async Task ShouldMatchNothing_WhenANamedEmployeeDoesNotExist(string? creatorUsername, string? assigneeUsername)
    {
        var bus = new StubToolBus().With(WorkOrderToolMother.Creator());

        var query = await new WorkOrderListFilter(null, creatorUsername, assigneeUsername).ToQueryAsync(bus);

        query.ShouldBeNull();
    }
}
