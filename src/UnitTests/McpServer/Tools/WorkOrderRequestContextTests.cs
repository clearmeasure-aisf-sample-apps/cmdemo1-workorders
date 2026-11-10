using ClearMeasure.Bootcamp.McpServer.Tools;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools;

[TestFixture]
public class WorkOrderRequestContextTests
{
    [Test]
    public async Task ShouldLoadTheWorkOrderAndTheExecutingUser_WhenBothExist()
    {
        var creator = WorkOrderToolMother.Creator();
        var draft = WorkOrderToolMother.Draft(creator);
        var bus = new StubToolBus().With(draft);

        var (context, error) = await WorkOrderRequestContext.LoadAsync(bus, "WO-1", "jsmith");

        error.ShouldBeNull();
        context.ShouldBe(new WorkOrderRequestContext(draft, creator));
    }

    [Test]
    public async Task ShouldExplain_WhenTheWorkOrderDoesNotExist()
    {
        var bus = new StubToolBus().With(WorkOrderToolMother.Creator());

        var (context, error) = await WorkOrderRequestContext.LoadAsync(bus, "WO-404", "jsmith");

        context.ShouldBeNull();
        error.ShouldBe("No work order found with number 'WO-404'.");
    }

    [Test]
    public async Task ShouldExplain_WhenTheExecutingUserDoesNotExist()
    {
        var bus = new StubToolBus().With(WorkOrderToolMother.Draft(WorkOrderToolMother.Creator()));

        var (context, error) = await WorkOrderRequestContext.LoadAsync(bus, "WO-1", "nobody");

        context.ShouldBeNull();
        error.ShouldBe("Employee with username 'nobody' not found.");
    }

    [TestCase("")]
    [TestCase("  ")]
    public async Task ShouldRequireANumber_WhenAnEditToolGivesNone(string number)
    {
        var bus = new StubToolBus();

        var (_, error) = await WorkOrderRequestContext.LoadForEditAsync(bus, number, "jsmith");

        error.ShouldBe("Work order number is required.");
        bus.SentRequests.ShouldBeEmpty();
    }
}
