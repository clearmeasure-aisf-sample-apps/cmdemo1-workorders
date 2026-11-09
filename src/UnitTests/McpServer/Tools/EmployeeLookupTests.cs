using ClearMeasure.Bootcamp.McpServer.Tools;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools;

[TestFixture]
public class EmployeeLookupTests
{
    [Test]
    public async Task ShouldFindTheEmployee_WhenTheUsernameExists()
    {
        var creator = WorkOrderToolMother.Creator();
        var bus = new StubToolBus().With(creator);

        var employee = await EmployeeLookup.FindAsync(bus, "jsmith");

        employee.ShouldBe(creator);
    }

    [Test]
    public async Task ShouldAnswerNull_WhenTheUsernameDoesNotExist()
    {
        var bus = new StubToolBus();

        var employee = await EmployeeLookup.FindAsync(bus, "nobody");

        employee.ShouldBeNull();
    }
}
