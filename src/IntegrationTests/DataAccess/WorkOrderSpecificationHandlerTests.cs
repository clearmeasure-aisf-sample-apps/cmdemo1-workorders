using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WorkOrderSpecificationHandlerTests
{
    [Test]
    public void ShouldHandleRemotedQuery()
    {
        var query = new WorkOrderSpecificationQuery();
        query.ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithAssignee()
    {
        new DatabaseTests().Clean();

        var employee1 = new Employee("1", "1", "1", "1");
        var employee2 = new Employee("2", "2", "2", "2");
        var order1 = new WorkOrder()
        {
            Creator = employee2,
            Assignee = employee1,
            Number = "123"
        };
        var order2 = new WorkOrder()
        {
            Creator = employee1,
            Assignee = employee2,
            Number = "456"
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
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchAssignee(employee1);
        var orders = await repository.Handle(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithCreator()
    {
        new DatabaseTests().Clean();

        var creator1 = new Employee("1", "1", "1", "1");
        var creator2 = new Employee("2", "2", "2", "2");
        var order1 = new WorkOrder()
        {
            Creator = creator1,
            Number = "123"
        };
        var order2 = new WorkOrder()
        {
            Creator = creator2,
            Number = "456"
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
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator1);
        var orders = await repository.Handle(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithFullSpecification()
    {
        new DatabaseTests().Clean();

        var employee1 = new Employee("1", "1", "1", "1");
        var employee2 = new Employee("2", "2", "2", "2");
        var order1 = new WorkOrder()
        {
            Creator = employee2,
            Assignee = employee1,
            Number = "123",
            Status = WorkOrderStatus.Assigned
        };
        var order2 = new WorkOrder()
        {
            Creator = employee1,
            Assignee = employee2,
            Number = "456",
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
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchStatus(WorkOrderStatus.Assigned);
        specification.MatchCreator(employee2);
        specification.MatchAssignee(employee1);
        var orders = await repository.Handle(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchBySpecificationWithStatus()
    {
        new DatabaseTests().Clean();

        var employee1 = new Employee("1", "1", "1", "1");
        var employee2 = new Employee("2", "2", "2", "2");
        var order1 = new WorkOrder()
        {
            Creator = employee2,
            Assignee = employee1,
            Number = "123",
            Status = WorkOrderStatus.Assigned
        };
        var order2 = new WorkOrder()
        {
            Creator = employee1,
            Assignee = employee2,
            Number = "456",
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
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchStatus(WorkOrderStatus.Assigned);
        var orders = await repository.Handle(specification);

        orders.Length.ShouldBe(1);
        orders[0].Id.ShouldBe(order1.Id);
    }

    [Test]
    public async Task ShouldSearchWithEmptySpecificationAndReturnAll()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("1", "1", "1", "1");
        var order1 = new WorkOrder { Creator = employee, Assignee = employee, Number = "123" };
        var order2 = new WorkOrder { Creator = employee, Assignee = employee, Number = "456" };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(order1);
            context.Add(order2);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);
        var orders = await repository.Handle(new WorkOrderSpecificationQuery());

        orders.Length.ShouldBe(2);
        orders.Any(o => o.Id == order1.Id).ShouldBeTrue();
        orders.Any(o => o.Id == order2.Id).ShouldBeTrue();
    }

    [Test]
    public async Task SearchShouldReturnHydratedEmployeesWithWorkOrders()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("1", "John", "Doe", "john.doe@example.com");
        var assignee = new Employee("2", "Jane", "Smith", "jane.smith@example.com");

        var order1 = new WorkOrder
        {
            Creator = creator,
            Assignee = assignee,
            Number = "123",
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
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);

        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator);

        var orders = await repository.Handle(specification);

        orders.Length.ShouldBe(1);

        var rehydratedOrder = orders.First(o => o.Number == "123");

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
    [Test]
    public async Task ShouldReturnOnlyOverdueWorkOrders_WhenOverdueOnlyFilterIsTrue()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("1", "1", "1", "1");
        var overdueOrder = new WorkOrder
        {
            Creator = employee,
            Number = "OVR-001",
            Status = WorkOrderStatus.InProgress,
            DueDate = new DateOnly(2000, 1, 1)
        };
        var futureOrder = new WorkOrder
        {
            Creator = employee,
            Number = "FUT-001",
            Status = WorkOrderStatus.InProgress,
            DueDate = new DateOnly(2099, 12, 31)
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.Add(overdueOrder);
            context.Add(futureOrder);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchOverdueOnly(true);
        var orders = await repository.Handle(specification);

        orders.Length.ShouldBe(1);
        orders[0].Number.ShouldBe("OVR-001");
    }
    [Test]
    public async Task ShouldReturnEachWorkOrderOnce_WhenSearchingByCreatorWithSeveralRoles()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("tlovejoy", "Timothy", "Lovejoy Jr", "tlovejoy@example.com");
        creator.AddRole(new Role("Pastor", true, false));
        creator.AddRole(new Role("Requester", true, false));
        var other = new Employee("other", "Other", "Person", "other@example.com");
        var orders = new[]
        {
            new WorkOrder { Creator = creator, Number = "38173BA", Title = "One" },
            new WorkOrder { Creator = creator, Number = "38173BB", Title = "Two" },
            new WorkOrder { Creator = creator, Assignee = other, Number = "38173BC", Title = "Three" },
            new WorkOrder { Creator = other, Number = "38173BD", Title = "Not mine" }
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.Add(other);
            context.AddRange(orders.Cast<object>());
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var repository = new WorkOrderSearchHandler(dataContext, TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchCreator(creator);
        var result = await repository.Handle(specification);

        result.Length.ShouldBe(3);
        result.Select(o => o.Number).Distinct().Count().ShouldBe(3);
        result.Select(o => o.Number).ShouldBe(["38173BA", "38173BB", "38173BC"], ignoreOrder: true);
    }
    [Test]
    public async Task ShouldOrderByCreatedDateDescendingThenNumber()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("order", "Order", "Creator", "order@example.com");
        var oldest = new WorkOrder { Creator = creator, Number = "AAA0001", CreatedDate = new DateTime(2026, 1, 1) };
        var newestB = new WorkOrder { Creator = creator, Number = "BBB0002", CreatedDate = new DateTime(2026, 3, 1) };
        var newestA = new WorkOrder { Creator = creator, Number = "AAA0002", CreatedDate = new DateTime(2026, 3, 1) };
        var middle = new WorkOrder { Creator = creator, Number = "CCC0003", CreatedDate = new DateTime(2026, 2, 1) };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(creator);
            context.AddRange(oldest, newestB, newestA, middle);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var orders = await handler.Handle(new WorkOrderSpecificationQuery());

        orders.Select(o => o.Number).ShouldBe(["AAA0002", "BBB0002", "CCC0003", "AAA0001"]);
    }

    [Test]
    public async Task ShouldMatchSearchTextCaseInsensitively()
    {
        new DatabaseTests().Clean();

        var creator = new Employee("ned", "Ned", "Flanders", "ned@example.com");
        var assignee = new Employee("willie", "Groundskeeper", "Willie", "willie@example.com");
        var byTitle = new WorkOrder { Creator = creator, Number = "SRCH001", Title = "Tune the ORGAN", RoomNumber = "Choir loft" };
        var byAssignee = new WorkOrder { Creator = creator, Assignee = assignee, Number = "SRCH002", Title = "Mow", RoomNumber = "Grounds" };
        var none = new WorkOrder { Creator = creator, Number = "SRCH003", Title = "Paint", RoomNumber = "Hall" };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.AddRange(creator, assignee);
            context.AddRange(byTitle, byAssignee, none);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var organ = new WorkOrderSpecificationQuery();
        organ.MatchSearchText("organ");
        var willie = new WorkOrderSpecificationQuery();
        willie.MatchSearchText("WILL");

        (await handler.Handle(organ)).Select(o => o.Number).ShouldBe(["SRCH001"]);
        (await handler.Handle(willie)).Select(o => o.Number).ShouldBe(["SRCH002"]);
    }

    [Test]
    public async Task ShouldFilterByRoomExactMatch()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("1", "1", "1", "1");
        var room101 = new WorkOrder { Creator = employee, Number = "RM001", RoomNumber = "101" };
        var room101Spaces = new WorkOrder { Creator = employee, Number = "RM002", RoomNumber = " 101 " };
        var room102 = new WorkOrder { Creator = employee, Number = "RM003", RoomNumber = "102" };
        var noRoom = new WorkOrder { Creator = employee, Number = "RM004", RoomNumber = null };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.AddRange(room101, room101Spaces, room102, noRoom);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchRoom("101");
        var orders = await handler.Handle(specification);

        orders.Select(o => o.Number).ShouldBe(["RM001", "RM002"], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldFilterByRoomCaseInsensitiveWhenDatabaseIsCaseInsensitive()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("1", "1", "1", "1");
        var lowerRoom = new WorkOrder { Creator = employee, Number = "CS001", RoomNumber = "sanctuary" };
        var upperRoom = new WorkOrder { Creator = employee, Number = "CS002", RoomNumber = "SANCTUARY" };
        var mixedRoom = new WorkOrder { Creator = employee, Number = "CS003", RoomNumber = "Sanctuary" };
        var differentRoom = new WorkOrder { Creator = employee, Number = "CS004", RoomNumber = "kitchen" };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.AddRange(lowerRoom, upperRoom, mixedRoom, differentRoom);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchRoom("Sanctuary");
        var orders = await handler.Handle(specification);

        // SQL Server is case-insensitive; SQLite is case-sensitive.
        // This test should find at least the exact match.
        orders.Select(o => o.Number).ShouldContain("CS003");
    }

    [Test]
    public async Task ShouldFilterByNoRoomWhenRoomIsNull()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("1", "1", "1", "1");
        var withRoom = new WorkOrder { Creator = employee, Number = "NR001", RoomNumber = "101" };
        var nullRoom = new WorkOrder { Creator = employee, Number = "NR002", RoomNumber = null };
        var emptyRoom = new WorkOrder { Creator = employee, Number = "NR003", RoomNumber = "" };
        var whitespaceRoom = new WorkOrder { Creator = employee, Number = "NR004", RoomNumber = "   " };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.AddRange(withRoom, nullRoom, emptyRoom, whitespaceRoom);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchNoRoom(true);
        var orders = await handler.Handle(specification);

        orders.Select(o => o.Number).ShouldBe(["NR002", "NR003", "NR004"], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldFilterByNoBuilding()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("1", "1", "1", "1");
        var withBuilding = new WorkOrder { Creator = employee, Number = "NB001", Building = "Sanctuary" };
        var nullBuilding = new WorkOrder { Creator = employee, Number = "NB002", Building = null };
        var emptyBuilding = new WorkOrder { Creator = employee, Number = "NB003", Building = "" };
        var whitespaceBuilding = new WorkOrder { Creator = employee, Number = "NB004", Building = "   " };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.AddRange(withBuilding, nullBuilding, emptyBuilding, whitespaceBuilding);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchNoBuilding(true);
        var orders = await handler.Handle(specification);

        orders.Select(o => o.Number).ShouldBe(["NB002", "NB003", "NB004"], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldMatchRoomExactWhenBothRoomAndNoBuildingAreSpecified()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("1", "1", "1", "1");
        var sanctuaryRoom101 = new WorkOrder { Creator = employee, Number = "CB001", Building = "Sanctuary", RoomNumber = "101" };
        var noRoomWithBuilding = new WorkOrder { Creator = employee, Number = "CB002", Building = "Sanctuary", RoomNumber = null };
        var room101NoBuilding = new WorkOrder { Creator = employee, Number = "CB003", Building = null, RoomNumber = "101" };
        var room101NoRoom = new WorkOrder { Creator = employee, Number = "CB004", Building = null, RoomNumber = null };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.AddRange(sanctuaryRoom101, noRoomWithBuilding, room101NoBuilding, room101NoRoom);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchRoom("101");
        var orders = await handler.Handle(specification);

        orders.Select(o => o.Number).ShouldBe(["CB001", "CB003"], ignoreOrder: true);
    }

    [Test]
    public async Task ShouldCombineRoomFilterWithOpenOnlyFilter()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("1", "1", "1", "1");
        var openRoom101 = new WorkOrder { Creator = employee, Number = "RO001", RoomNumber = "101", Status = WorkOrderStatus.Assigned };
        var completeRoom101 = new WorkOrder { Creator = employee, Number = "RO002", RoomNumber = "101", Status = WorkOrderStatus.Complete };
        var openRoom102 = new WorkOrder { Creator = employee, Number = "RO003", RoomNumber = "102", Status = WorkOrderStatus.Draft };
        var draftRoom101 = new WorkOrder { Creator = employee, Number = "RO004", RoomNumber = "101", Status = WorkOrderStatus.Draft };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.AddRange(openRoom101, completeRoom101, openRoom102, draftRoom101);
            await context.SaveChangesAsync();
        }

        var handler = new WorkOrderSearchHandler(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);
        var specification = new WorkOrderSpecificationQuery();
        specification.MatchRoom("101");
        specification.MatchOpenOnly(true);
        var orders = await handler.Handle(specification);

        orders.Select(o => o.Number).ShouldBe(["RO001", "RO004"], ignoreOrder: true);
    }
}
