using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WorkOrderQueryHandlerTests
{
    [Test]
    public async Task ShouldGetWorkOrderByNumber()
    {
        var number123 = TestData.Number("Q");
        var number456 = TestData.Number("Q");
        var creator = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var order1 = WorkOrderBuilder.Create().CreatedBy(creator).WithNumber(number123).Build();
        var order2 = WorkOrderBuilder.Create().CreatedBy(creator).WithNumber(number456).Build();

        await GivenPersistedAsync(creator, order1, order2);

        var found = await WhenGetByNumbers(number123, number456);

        found[0].Id.ShouldBe(order1.Id);
        found[1].Id.ShouldBe(order2.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithAssignee()
    {
        var employee1 = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var employee2 = EmployeeBuilder.Create(TestData.UserName("employee2"))
            .WithFirstName("2").WithLastName("2").WithEmail("2").Build();
        var order1 = WorkOrderBuilder.Create().CreatedBy(employee2).AssignedTo(employee1)
            .WithNumber(TestData.Number("Q")).Build();
        var order2 = WorkOrderBuilder.Create().CreatedBy(employee1).AssignedTo(employee2)
            .WithNumber(TestData.Number("Q")).Build();

        await GivenPersistedAsync(employee1, employee2, order1, order2);

        var orders = await WhenSearchMatchingAssignee(employee1);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithCreator()
    {
        var creator1 = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var creator2 = EmployeeBuilder.Create(TestData.UserName("employee2"))
            .WithFirstName("2").WithLastName("2").WithEmail("2").Build();
        var order1 = WorkOrderBuilder.Create().CreatedBy(creator1).WithNumber(TestData.Number("Q")).Build();
        var order2 = WorkOrderBuilder.Create().CreatedBy(creator2).WithNumber(TestData.Number("Q")).Build();

        await GivenPersistedAsync(creator1, creator2, order1, order2);

        var orders = await WhenSearchMatchingCreator(creator1);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithFullSpecification()
    {
        var employee1 = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var employee2 = EmployeeBuilder.Create(TestData.UserName("employee2"))
            .WithFirstName("2").WithLastName("2").WithEmail("2").Build();
        var order1 = WorkOrderBuilder.For(employee2, WorkOrderStatus.Assigned).AssignedTo(employee1)
            .WithNumber(TestData.Number("Q")).Build();
        var order2 = WorkOrderBuilder.For(employee1, WorkOrderStatus.Draft).AssignedTo(employee2)
            .WithNumber(TestData.Number("Q")).Build();

        await GivenPersistedAsync(employee1, employee2, order1, order2);

        var orders = await WhenSearchFullSpec(employee2, employee1, WorkOrderStatus.Assigned);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithStatus()
    {
        var employee1 = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var employee2 = EmployeeBuilder.Create(TestData.UserName("employee2"))
            .WithFirstName("2").WithLastName("2").WithEmail("2").Build();
        var order1 = WorkOrderBuilder.For(employee2, WorkOrderStatus.Assigned).AssignedTo(employee1)
            .WithNumber(TestData.Number("Q")).Build();
        var order2 = WorkOrderBuilder.For(employee1, WorkOrderStatus.Draft).AssignedTo(employee2)
            .WithNumber(TestData.Number("Q")).Build();

        await GivenPersistedAsync(employee1, employee2, order1, order2);

        var orders = await WhenSearchMatchingStatusAmong(WorkOrderStatus.Assigned, order1.Id, order2.Id);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchWithEmptySpecificationAndReturnAll()
    {
        var employee = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("1").WithLastName("1").WithEmail("1").Build();
        var order1 = WorkOrderBuilder.Create().CreatedBy(employee).AssignedTo(employee)
            .WithNumber(TestData.Number("Q")).Build();
        var order2 = WorkOrderBuilder.Create().CreatedBy(employee).AssignedTo(employee)
            .WithNumber(TestData.Number("Q")).Build();

        await GivenPersistedAsync(order1, order2);

        var orders = await WhenSearchEmpty();

        orders.Any(o => o.Id == order1.Id).ShouldBeTrue();
        orders.Any(o => o.Id == order2.Id).ShouldBeTrue();
    }

    [Test]
    public async Task SearchShouldReturnHydratedEmployeesWithWorkOrders()
    {
        var number123 = TestData.Number("Q");
        var creator = EmployeeBuilder.Create(TestData.UserName("employee1"))
            .WithFirstName("John").WithLastName("Doe").WithEmail("john.doe@example.com").Build();
        var assignee = EmployeeBuilder.Create(TestData.UserName("employee2"))
            .WithFirstName("Jane").WithLastName("Smith").WithEmail("jane.smith@example.com").Build();
        var order1 = WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).AssignedTo(assignee)
            .WithNumber(number123).WithTitle("Fix plumbing")
            .WithDescription("Fix the plumbing in room 101").InRoom("101").Build();

        await GivenPersistedAsync(creator, assignee, order1);

        var orders = await WhenSearchMatchingCreator(creator);

        ThenHydratedEmployeesMatch(orders, number123, creator, assignee);
    }

    private static async Task GivenPersistedAsync(params object[] entities)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }

    private static async Task<WorkOrder[]> WhenGetByNumbers(string number123, string number456)
    {
        var repository = new WorkOrderQueryHandler(TestHost.GetRequiredService<DataContext>());
        var order123 = (await repository.GetWorkOrderAsync(number123))!;
        var order456 = (await repository.GetWorkOrderAsync(number456))!;
        return [order123, order456];
    }

    private static async Task<WorkOrder[]> WhenSearchMatchingAssignee(Employee assignee)
    {
        var repository = new WorkOrderQueryHandler(TestHost.GetRequiredService<DataContext>());
        var specification = new WorkOrderSearchSpecification();
        specification.MatchAssignee(assignee);
        return await repository.GetWorkOrdersAsync(specification);
    }

    private static async Task<WorkOrder[]> WhenSearchMatchingCreator(Employee creator)
    {
        var repository = new WorkOrderQueryHandler(TestHost.GetRequiredService<DataContext>());
        var specification = new WorkOrderSearchSpecification();
        specification.MatchCreator(creator);
        return await repository.GetWorkOrdersAsync(specification);
    }

    private static async Task<WorkOrder[]> WhenSearchFullSpec(Employee creator, Employee assignee, WorkOrderStatus status)
    {
        var repository = new WorkOrderQueryHandler(TestHost.GetRequiredService<DataContext>());
        var specification = new WorkOrderSearchSpecification();
        specification.MatchStatus(status);
        specification.MatchCreator(creator);
        specification.MatchAssignee(assignee);
        return await repository.GetWorkOrdersAsync(specification);
    }

    private static async Task<WorkOrder[]> WhenSearchMatchingStatusAmong(WorkOrderStatus status, Guid id1, Guid id2)
    {
        var repository = new WorkOrderQueryHandler(TestHost.GetRequiredService<DataContext>());
        var specification = new WorkOrderSearchSpecification();
        specification.MatchStatus(status);
        return (await repository.GetWorkOrdersAsync(specification))
            .Where(o => o.Id == id1 || o.Id == id2).ToArray();
    }

    private static async Task<WorkOrder[]> WhenSearchEmpty()
    {
        var repository = new WorkOrderQueryHandler(TestHost.GetRequiredService<DataContext>());
        return await repository.GetWorkOrdersAsync(new WorkOrderSearchSpecification());
    }

    private static void ThenHydratedEmployeesMatch(WorkOrder[] orders, string number, Employee creator, Employee assignee)
    {
        orders.Length.ShouldBe(1);
        var rehydratedOrder = orders.First(o => o.Number == number);
        rehydratedOrder.Creator.ShouldNotBeNull();
        rehydratedOrder.Assignee.ShouldNotBeNull();
        rehydratedOrder.Creator.Id.ShouldBe(creator.Id);
        rehydratedOrder.Creator.FirstName.ShouldBe(creator.FirstName);
        rehydratedOrder.Creator.LastName.ShouldBe(creator.LastName);
        rehydratedOrder.Creator.EmailAddress.ShouldBe(creator.EmailAddress);
        rehydratedOrder.Assignee.Id.ShouldBe(assignee.Id);
        rehydratedOrder.Assignee.FirstName.ShouldBe(assignee.FirstName);
        rehydratedOrder.Assignee.LastName.ShouldBe(assignee.LastName);
        rehydratedOrder.Assignee.EmailAddress.ShouldBe(assignee.EmailAddress);
    }
}
