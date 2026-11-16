using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.WorkOrderSpecification;

[TestFixture]
public class EmployeeHydrationTests
{
    [Test]
    public async Task SearchShouldReturnHydratedEmployeesWithWorkOrders()
    {
        var number123 = TestData.Number("S");
        var creator = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john.doe@example.com").Build();
        var assignee = EmployeeBuilder.Create(TestData.UserName("employee2"))
            .WithFirstName("Jane").WithLastName("Smith").WithEmail("jane.smith@example.com").Build();
        await WorkOrderQuerySteps.SaveOrdersAsync(creator,
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).AssignedTo(assignee)
                .WithNumber(number123).WithTitle("Fix plumbing")
                .WithDescription("Fix the plumbing in room 101").InRoom("101").Build());

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator);
        var orders = await WorkOrderQuerySteps.SearchAsync(specification);

        orders.Length.ShouldBe(1);
        ThenEmployeesAreHydrated(orders.First(o => o.Number == number123), creator, assignee);
    }

    private static void ThenEmployeesAreHydrated(WorkOrder order, Employee creator, Employee assignee)
    {
        order.Creator.ShouldNotBeNull();
        order.Assignee.ShouldNotBeNull();
        order.Creator.Id.ShouldBe(creator.Id);
        order.Creator.FirstName.ShouldBe(creator.FirstName);
        order.Creator.LastName.ShouldBe(creator.LastName);
        order.Creator.EmailAddress.ShouldBe(creator.EmailAddress);
        order.Assignee.Id.ShouldBe(assignee.Id);
        order.Assignee.FirstName.ShouldBe(assignee.FirstName);
        order.Assignee.LastName.ShouldBe(assignee.LastName);
        order.Assignee.EmailAddress.ShouldBe(assignee.EmailAddress);
    }
}
