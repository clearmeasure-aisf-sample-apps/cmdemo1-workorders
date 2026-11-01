using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
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

        var creator = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var order1 = new WorkOrder()
        {
            Creator = creator,
            Number = number123
        };
        var order2 = new WorkOrder()
        {
            Creator = creator,
            Number = number456
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderQueryHandler(dataContext);
        var order123 = (await repository.GetWorkOrderAsync(number123))!;
        var order456 = (await repository.GetWorkOrderAsync(number456))!;

        order123.Id.ShouldBe(order1.Id);
        order456.Id.ShouldBe(order2.Id);
    }


    [Test]
    public async Task ShouldSearchBySpecificationWithAssignee()
    {
        var number123 = TestData.Number("Q");
        var number456 = TestData.Number("Q");

        var employee1 = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var employee2 = new Employee(TestData.UserName("employee2"), "2", "2", "2");
        var order1 = new WorkOrder()
        {
            Creator = employee2,
            Assignee = employee1,
            Number = number123
        };
        var order2 = new WorkOrder()
        {
            Creator = employee1,
            Assignee = employee2,
            Number = number456
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee1);
            context.Add(employee2);
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderQueryHandler(dataContext);
        var specification = new WorkOrderSearchSpecification();
        specification.MatchAssignee(employee1);
        var orders = await repository.GetWorkOrdersAsync(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithCreator()
    {
        var number123 = TestData.Number("Q");
        var number456 = TestData.Number("Q");

        var creator1 = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var creator2 = new Employee(TestData.UserName("employee2"), "2", "2", "2");
        var order1 = new WorkOrder()
        {
            Creator = creator1,
            Number = number123
        };
        var order2 = new WorkOrder()
        {
            Creator = creator2,
            Number = number456
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator1);
            context.Add(creator2);
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderQueryHandler(dataContext);
        var specification = new WorkOrderSearchSpecification();
        specification.MatchCreator(creator1);
        var orders = await repository.GetWorkOrdersAsync(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithFullSpecification()
    {
        var number123 = TestData.Number("Q");
        var number456 = TestData.Number("Q");

        var employee1 = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var employee2 = new Employee(TestData.UserName("employee2"), "2", "2", "2");
        var order1 = new WorkOrder()
        {
            Creator = employee2,
            Assignee = employee1,
            Number = number123,
            Status = WorkOrderStatus.Assigned
        };
        var order2 = new WorkOrder()
        {
            Creator = employee1,
            Assignee = employee2,
            Number = number456,
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee1);
            context.Add(employee2);
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderQueryHandler(dataContext);
        var specification = new WorkOrderSearchSpecification();
        specification.MatchStatus(WorkOrderStatus.Assigned);
        specification.MatchCreator(employee2);
        specification.MatchAssignee(employee1);
        var orders = await repository.GetWorkOrdersAsync(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithStatus()
    {
        var number123 = TestData.Number("Q");
        var number456 = TestData.Number("Q");

        var employee1 = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var employee2 = new Employee(TestData.UserName("employee2"), "2", "2", "2");
        var order1 = new WorkOrder()
        {
            Creator = employee2,
            Assignee = employee1,
            Number = number123,
            Status = WorkOrderStatus.Assigned
        };
        var order2 = new WorkOrder()
        {
            Creator = employee1,
            Assignee = employee2,
            Number = number456,
            Status = WorkOrderStatus.Draft
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee1);
            context.Add(employee2);
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderQueryHandler(dataContext);
        var specification = new WorkOrderSearchSpecification();
        specification.MatchStatus(WorkOrderStatus.Assigned);
        var orders = (await repository.GetWorkOrdersAsync(specification))
            .Where(o => o.Id == order1.Id || o.Id == order2.Id).ToArray();

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchWithEmptySpecificationAndReturnAll()
    {
        var number123 = TestData.Number("Q");
        var number456 = TestData.Number("Q");

        var employee = new Employee(TestData.UserName("employee1"), "1", "1", "1");
        var order1 = new WorkOrder { Creator = employee, Assignee = employee, Number = number123 };
        var order2 = new WorkOrder { Creator = employee, Assignee = employee, Number = number456 };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderQueryHandler(dataContext);
        var orders = await repository.GetWorkOrdersAsync(new WorkOrderSearchSpecification());

        orders.Any(o => o.Id == order1.Id).ShouldBeTrue();
        orders.Any(o => o.Id == order2.Id).ShouldBeTrue();
    }


    [Test]
    public async Task SearchShouldReturnHydratedEmployeesWithWorkOrders()
    {
        var number123 = TestData.Number("Q");

        var creator = new Employee(TestData.UserName("employee1"), "John", "Doe", "john.doe@example.com");
        var assignee = new Employee(TestData.UserName("employee2"), "Jane", "Smith", "jane.smith@example.com");

        var order1 = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = number123,
            Title = "Fix plumbing",
            Description = "Fix the plumbing in room 101",
            RoomNumber = "101",
            Status = WorkOrderStatus.InProgress
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(assignee);
            context.Add(order1);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderQueryHandler(dataContext);

        var specification = new WorkOrderSearchSpecification();
        specification.MatchCreator(creator);

        var orders = await repository.GetWorkOrdersAsync(specification);

        orders.Length.ShouldBe(1);

        var rehydratedOrder = orders.First(o => o.Number == number123);

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
